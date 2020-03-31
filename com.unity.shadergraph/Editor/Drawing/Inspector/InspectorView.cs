﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Data.Interfaces;
using Drawing.Views;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Drawing.Inspector
{
    class InspectorView : GraphSubWindow
    {
        // References
        // #TODO: Remove concrete reference to SG GraphData object
        readonly GraphData m_GraphData;
        readonly IList<Type> m_PropertyDrawerList = new List<Type>();

        // Used to handle drawing the default settings of the graph that the Inspector is targeting
        private Type m_GraphSettingsPropertyDrawerType = null;
        private object m_defaultSettingsData;

        public int currentlyDisplayedPropertyCount { get; private set; } = 0;

        protected override string windowTitle => "Inspector";
        protected override string elementName => "InspectorView";
        protected override string styleName => "InspectorView";

        public InspectorView(object defaultSettingsData, GraphView graphView) : base(graphView)
        {
            m_defaultSettingsData = defaultSettingsData;

            // Register property drawer types here
            RegisterPropertyDrawer(typeof(BoolPropertyDrawer));
            RegisterPropertyDrawer(typeof(EnumPropertyDrawer));
            RegisterPropertyDrawer(typeof(TextPropertyDrawer));
            RegisterPropertyDrawer(typeof(Vector2PropertyDrawer));
            RegisterPropertyDrawer(typeof(Vector3PropertyDrawer));
            RegisterPropertyDrawer(typeof(Vector4PropertyDrawer));
            RegisterPropertyDrawer(typeof(MatrixPropertyDrawer));
            RegisterPropertyDrawer(typeof(ColorPropertyDrawer));
            RegisterPropertyDrawer(typeof(GradientPropertyDrawer));
            RegisterPropertyDrawer(typeof(Texture2DPropertyDrawer));
            RegisterPropertyDrawer(typeof(Texture2DArrayPropertyDrawer));
            RegisterPropertyDrawer(typeof(Texture3DPropertyDrawer));
            RegisterPropertyDrawer(typeof(CubemapPropertyDrawer));
            RegisterPropertyDrawer(typeof(ShaderInputPropertyDrawer));
            RegisterPropertyDrawer(typeof(GraphDataPropertyDrawer));

            if (IsPropertyTypeHandled(defaultSettingsData.GetType(), out var propertyDrawerToUse))
            {
                m_GraphSettingsPropertyDrawerType = propertyDrawerToUse;
            }
        }

#region PropertyDrawing
        void RegisterPropertyDrawer(Type propertyDrawerType)
        {
            // #TODO: Look into the right way to warn the user that there are errors they should probably be aware of

            if(typeof(IPropertyDrawer).IsAssignableFrom(propertyDrawerType) == false)
                throw new Exception("Attempted to register a property drawer that doesn't inherit from IPropertyDrawer!");

            var customAttribute = propertyDrawerType.GetCustomAttribute<SGPropertyDrawer>();
            if(customAttribute != null)
                m_PropertyDrawerList.Add(propertyDrawerType);
            else
                throw new Exception("Attempted to register a property drawer that isn't marked up with the SGPropertyDrawer attribute!");
        }

        bool IsPropertyTypeHandled(Type typeOfProperty, out Type propertyDrawerToUse)
        {
            propertyDrawerToUse = null;

            // Check to see if a property drawer has been registered that handles this type
            foreach (var propertyDrawerType in m_PropertyDrawerList)
            {
                var typeHandledByPropertyDrawer = propertyDrawerType.GetCustomAttribute<SGPropertyDrawer>();
                // Numeric types and boolean wrapper types like ToggleData handled here
                if (typeHandledByPropertyDrawer.propertyType == typeOfProperty)
                {
                    propertyDrawerToUse = propertyDrawerType;
                    return true;
                }
                // Enums are weird and need to be handled explicitly as done below as their runtime type isn't the same as System.Enum
                else if (typeHandledByPropertyDrawer.propertyType == typeOfProperty.BaseType)
                {
                    propertyDrawerToUse = propertyDrawerType;
                    return true;
                }
            }

            return false;
        }

#endregion

#region Selection
        public void Update()
        {
            currentlyDisplayedPropertyCount = selection.Count;

            // Remove current properties
            for (int i = 0; i < m_ContentContainer.childCount; ++i)
            {
                var child = m_ContentContainer.Children().ElementAt(i);
                m_ContentContainer.Remove(child);
            }

            var propertySheet = new PropertySheet();
            if(selection.Count == 0)
            {
                ShowGraphSettings(propertySheet);
                return;
            }

            if(selection.Count > 1)
            {
                subTitle = $"{selection.Count} Objects.";
            }

            try
            {
                foreach (var selectable in selection)
                {
                    DrawSelection(selectable, propertySheet);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            m_ContentContainer.Add(propertySheet);
            m_ContentContainer.MarkDirtyRepaint();
        }

        private void DrawSelection(ISelectable selectable, PropertySheet propertySheet)
        {
            object dataObject = null;
            var inspectable = (IInspectable) selectable;
            if (inspectable == null)
            {
                throw new InvalidCastException("Failed to cast selection to Inspectable in the inspector. Please make sure that your selection is also implementing the IInspectable interface!");
                return;
            }

            DrawInspectable(propertySheet, inspectable);
        }

        private void DrawInspectable(PropertySheet propertySheet, IInspectable inspectable)
        {
            var dataObject = inspectable.GetObjectToInspect();
            if (dataObject == null)
                throw new NullReferenceException("DataObject returned by Inspectable is null!");

            var properties = inspectable.GetPropertyInfo();
            if (properties == null)
                throw new NullReferenceException("PropertyInfos returned by Inspectable is null!");

            foreach (var propertyInfo in properties)
            {
                var attribute = propertyInfo.GetCustomAttribute<Inspectable>();
                if (attribute == null)
                    continue;

                var propertyType = propertyInfo.PropertyType;

                if (IsPropertyTypeHandled(propertyType, out var propertyDrawerTypeToUse))
                {
                    var propertyDrawerInstance = (IPropertyDrawer) Activator.CreateInstance(propertyDrawerTypeToUse);
                    // Supply any required data to this particular kind of property drawer
                    inspectable.SupplyDataToPropertyDrawer(propertyDrawerInstance, this.Update);
                    var propertyGUI = propertyDrawerInstance.DrawProperty(propertyInfo, dataObject, attribute);
                    propertySheet.Add(propertyGUI);
                }
            }
        }

        // This should be implemented by any inspector class that wants to define its own GraphSettings
        // which for SG, is a representation of the settings in GraphData
        protected virtual void ShowGraphSettings(PropertySheet propertySheet)
        {
            var graphEditorView = m_GraphView.GetFirstAncestorOfType<GraphEditorView>();
            if(graphEditorView == null)
                return;

            subTitle = $"{graphEditorView.assetName} (Graph)";

            DrawInspectable(propertySheet, (IInspectable)graphView);

            // #TODO - Refactor, shouldn't this just be a property on the graph data object itself?
            var precisionField = new EnumField((Enum)m_GraphData.concretePrecision);
            precisionField.RegisterValueChangedCallback(evt =>
            {
                m_GraphData.owner.RegisterCompleteObjectUndo("Change Precision");
                if (m_GraphData.concretePrecision == (ConcretePrecision)evt.newValue)
                    return;

                m_GraphData.concretePrecision = (ConcretePrecision)evt.newValue;
                var nodeList = m_GraphView.Query<MaterialNodeView>().ToList();
                graphEditorView.colorManager.SetNodesDirty(nodeList);

                m_GraphData.ValidateGraph();
                graphEditorView.colorManager.UpdateNodeViews(nodeList);
                foreach (var node in m_GraphData.GetNodes<AbstractMaterialNode>())
                {
                    node.Dirty(ModificationScope.Graph);
                }
            });

            var sheet = new PropertySheet();
            sheet.Add(new PropertyRow(new Label("Precision")), (row) =>
            {
                row.Add(precisionField);
            });
            m_ContentContainer.Add(sheet);
        }
#endregion
    }
}
