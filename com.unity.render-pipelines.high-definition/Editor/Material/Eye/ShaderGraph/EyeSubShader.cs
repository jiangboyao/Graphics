using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class EyeSubShader : IEyeSubShader
    {
        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "META",
            TemplateName = "EyePass.template",
            MaterialName = "Eye",
            ShaderPassName = "SHADERPASS_LIGHT_TRANSPORT",
            CullOverride = "Cull Off",
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
            },
            PixelShaderSlots = new List<int>()
            {
                EyeMasterNode.AlbedoSlotId,
                EyeMasterNode.SpecularOcclusionSlotId,
                EyeMasterNode.NormalSlotId,
                EyeMasterNode.SmoothnessSlotId,
                EyeMasterNode.AmbientOcclusionSlotId,
                EyeMasterNode.RefractionMaskSlotId,
                EyeMasterNode.DiffusionProfileHashSlotId,
                EyeMasterNode.SubsurfaceMaskSlotId,
                EyeMasterNode.ThicknessSlotId,
                EyeMasterNode.TangentSlotId,
                EyeMasterNode.AnisotropySlotId,
                EyeMasterNode.EmissionSlotId,
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.IrisNormalSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                //EyeMasterNode.PositionSlotId
            },
            UseInPreview = false,
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "EyePass.template",
            MaterialName = "Eye",
            ShaderPassName = "SHADERPASS_SHADOWS",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            ZClipOverride = HDSubShaderUtilities.zClipShadowCaster,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                EyeMasterNode.PositionSlotId
            },
            UseInPreview = false,
        };

        Pass m_SceneSelectionPass = new Pass()
        {
            Name = "SceneSelectionPass",
            LightMode = "SceneSelectionPass",
            TemplateName = "EyePass.template",
            MaterialName = "Eye",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ColorMaskOverride = "ColorMask 0",
            ExtraDefines = new List<string>()
            {
                "#define SCENESELECTIONPASS",
                "#pragma editor_sync_compilation",
            },            
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                EyeMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_PassDepthForwardOnly = new Pass()
        {
            Name = "DepthForwardOnly",
            LightMode = "DepthForwardOnly",
            TemplateName = "EyePass.template",
            MaterialName = "Eye",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ZWriteOverride = "ZWrite On",
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesForwardMaterialDepthOrMotion,
            
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                EyeMasterNode.NormalSlotId,
                EyeMasterNode.SmoothnessSlotId,
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            },

            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                "AttributesMesh.uv3",           // DEBUG_DISPLAY

                "FragInputs.tangentToWorld",
                "FragInputs.positionRWS",
                "FragInputs.texCoord0",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
                "FragInputs.texCoord3",
                "FragInputs.color",
            },

            VertexShaderSlots = new List<int>()
            {
                EyeMasterNode.PositionSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as EyeMasterNode;
                HDSubShaderUtilities.SetStencilStateForDepth(ref pass);
            }           
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "MotionVectors",
            LightMode = "MotionVectors",
            TemplateName = "EyePass.template",
            MaterialName = "Eye",
            ShaderPassName = "SHADERPASS_MOTION_VECTORS",
            ExtraDefines = HDSubShaderUtilities.s_ExtraDefinesForwardMaterialDepthOrMotion,
            CullOverride = HDSubShaderUtilities.defaultCullMode,
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                "AttributesMesh.uv3",           // DEBUG_DISPLAY

                "FragInputs.tangentToWorld",
                "FragInputs.positionRWS",
                "FragInputs.texCoord0",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
                "FragInputs.texCoord3",
                "FragInputs.color",
            },
            PixelShaderSlots = new List<int>()
            {
                EyeMasterNode.NormalSlotId,
                EyeMasterNode.SmoothnessSlotId,
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                EyeMasterNode.PositionSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as EyeMasterNode;
                HDSubShaderUtilities.SetStencilStateForMotionVector(ref pass);
            }
        };

        Pass m_PassForwardOnly = new Pass()
        {
            Name = "ForwardOnly",
            LightMode = "ForwardOnly",
            TemplateName = "EyePass.template",
            MaterialName = "Eye",
            ShaderPassName = "SHADERPASS_FORWARD",
            CullOverride = HDSubShaderUtilities.cullModeForward,
            ZTestOverride = HDSubShaderUtilities.zTestDepthEqualForOpaque,
            ZWriteOverride = HDSubShaderUtilities.ZWriteDefault,
            // ExtraDefines are set when the pass is generated
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                "AttributesMesh.uv3",           // DEBUG_DISPLAY

                "FragInputs.tangentToWorld",
                "FragInputs.positionRWS",
                "FragInputs.texCoord0",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
                "FragInputs.texCoord3",
                "FragInputs.color",
            },
            PixelShaderSlots = new List<int>()
            {
                EyeMasterNode.AlbedoSlotId,
                EyeMasterNode.SpecularOcclusionSlotId,
                EyeMasterNode.NormalSlotId,
                EyeMasterNode.BentNormalSlotId,
                EyeMasterNode.SmoothnessSlotId,
                EyeMasterNode.AmbientOcclusionSlotId,
                EyeMasterNode.RefractionMaskSlotId,
                EyeMasterNode.DiffusionProfileHashSlotId,
                EyeMasterNode.SubsurfaceMaskSlotId,
                EyeMasterNode.ThicknessSlotId,
                EyeMasterNode.TangentSlotId,
                EyeMasterNode.AnisotropySlotId,
                EyeMasterNode.EmissionSlotId,
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.LightingSlotId,
                EyeMasterNode.BackLightingSlotId,
                EyeMasterNode.DepthOffsetSlotId,
                EyeMasterNode.IrisNormalSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                EyeMasterNode.PositionSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as EyeMasterNode;
                HDSubShaderUtilities.SetStencilStateForForward(ref pass);
                HDSubShaderUtilities.SetBlendModeForForward(ref pass);

                pass.ExtraDefines.Remove("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");                

                if (masterNode.surfaceType == SurfaceType.Opaque)
                {
                    if (masterNode.alphaTest.isOn)
                    {
                        // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                        // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                        pass.ExtraDefines.Add("#ifndef DEBUG_DISPLAY\n#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST\n#endif");
                        pass.ZTestOverride = "ZTest Equal";
                    }
                    else
                        pass.ZTestOverride = null;
                }
            }
        };

        public int GetPreviewPassIndex() { return 0; }

        private static HashSet<string> GetActiveFieldsFromMasterNode(AbstractMaterialNode iMasterNode, Pass pass)
        {
            HashSet<string> activeFields = new HashSet<string>();

            EyeMasterNode masterNode = iMasterNode as EyeMasterNode;
            if (masterNode == null)
            {
                return activeFields;
            }

            if (masterNode.doubleSidedMode != DoubleSidedMode.Disabled)
            {
                if (pass.ShaderPassName != "SHADERPASS_MOTION_VECTORS")   // HACK to get around lack of a good interpolator dependency system
                {                                                   // we need to be able to build interpolators using multiple input structs
                                                                    // also: should only require isFrontFace if Normals are required...
                    // Important: the following is used in SharedCode.template.hlsl for determining the normal flip mode
                    activeFields.Add("FragInputs.isFrontFace");
                }
            }

            switch (masterNode.materialType)
            {
                case EyeMasterNode.MaterialType.EyeGames:
                    activeFields.Add("Material.EyeGames");
                    break;
                case EyeMasterNode.MaterialType.EyeCinematics:
                    activeFields.Add("Material.EyeCinematics");
                    break;
                default:
                    UnityEngine.Debug.LogError("Unknown material type: " + masterNode.materialType);
                    break;
            }

            if (masterNode.alphaTest.isOn)
            {
                if (pass.PixelShaderUsesSlot(EyeMasterNode.AlphaClipThresholdSlotId))
                {
                    activeFields.Add("AlphaTest");
                }
            }

            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                if (masterNode.transparencyFog.isOn)
                {
                    activeFields.Add("AlphaFog");
                }

                if (masterNode.blendPreserveSpecular.isOn)
                {
                    activeFields.Add("BlendMode.PreserveSpecular");
                }
            }

            if (!masterNode.receiveDecals.isOn)
            {
                activeFields.Add("DisableDecals");
            }

            if (!masterNode.receiveSSR.isOn)
            {
                activeFields.Add("DisableSSR");
            }

            if (masterNode.energyConservingSpecular.isOn)
            {
                activeFields.Add("Specular.EnergyConserving");
            }

            if (masterNode.transmission.isOn)
            {
                activeFields.Add("Material.Transmission");
            }

            if (masterNode.subsurfaceScattering.isOn && masterNode.surfaceType != SurfaceType.Transparent)
            {
                activeFields.Add("Material.SubsurfaceScattering");
            }

            if (masterNode.IsSlotConnected(EyeMasterNode.BentNormalSlotId) && pass.PixelShaderUsesSlot(EyeMasterNode.BentNormalSlotId))
            {
                activeFields.Add("BentNormal");
            }

            if (masterNode.IsSlotConnected(EyeMasterNode.TangentSlotId) && pass.PixelShaderUsesSlot(EyeMasterNode.TangentSlotId))
            {
                activeFields.Add("Tangent");
            }

            switch (masterNode.specularOcclusionMode)
            {
                case SpecularOcclusionMode.Off:
                    break;
                case SpecularOcclusionMode.FromAO:
                    activeFields.Add("SpecularOcclusionFromAO");
                    break;
                case SpecularOcclusionMode.FromAOAndBentNormal:
                    activeFields.Add("SpecularOcclusionFromAOBentNormal");
                    break;
                case SpecularOcclusionMode.Custom:
                    activeFields.Add("SpecularOcclusionCustom");
                    break;
                default:
                    break;
            }

            if (pass.PixelShaderUsesSlot(EyeMasterNode.AmbientOcclusionSlotId))
            {
                var occlusionSlot = masterNode.FindSlot<Vector1MaterialSlot>(EyeMasterNode.AmbientOcclusionSlotId);

                bool connected = masterNode.IsSlotConnected(EyeMasterNode.AmbientOcclusionSlotId);
                if (connected || occlusionSlot.value != occlusionSlot.defaultValue)
                {
                    activeFields.Add("AmbientOcclusion");
                }
            }

            if (masterNode.IsSlotConnected(EyeMasterNode.LightingSlotId) && pass.PixelShaderUsesSlot(EyeMasterNode.LightingSlotId))
            {
                activeFields.Add("LightingGI");
            }
            if (masterNode.IsSlotConnected(EyeMasterNode.BackLightingSlotId) && pass.PixelShaderUsesSlot(EyeMasterNode.BackLightingSlotId))
            {
                activeFields.Add("BackLightingGI");
            }

            if (masterNode.depthOffset.isOn && pass.PixelShaderUsesSlot(EyeMasterNode.DepthOffsetSlotId))
                activeFields.Add("DepthOffset");

            return activeFields;
        }

        private static bool GenerateShaderPassLit(EyeMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                pass.OnGeneratePass(masterNode);

                // apply master node options to active fields
                HashSet<string> activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                // use standard shader pass generation
                bool vertexActive = masterNode.IsSlotConnected(EyeMasterNode.PositionSlotId);
                return HDSubShaderUtilities.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths, vertexActive);
            }
            else
            {
                return false;
            }
        }

        public string GetSubshader(IMasterNode iMasterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // EyeSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("059cc3132f0336e40886300f3d2d7f12"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as EyeMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
                bool transparent = !opaque;

                // Add tags at the SubShader level
                var renderingPass = masterNode.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, masterNode.sortPriority, masterNode.alphaTest.isOn);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDLitShader, queue);

                GenerateShaderPassLit(masterNode, m_PassMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_SceneSelectionPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_PassShadowCaster, mode, subShader, sourceAssetDependencyPaths);

                GenerateShaderPassLit(masterNode, m_PassDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_PassMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                // Assign define here based on opaque or transparent to save some variant
                m_PassForwardOnly.ExtraDefines = opaque ? HDSubShaderUtilities.s_ExtraDefinesForwardOpaque : HDSubShaderUtilities.s_ExtraDefinesForwardTransparent;
                GenerateShaderPassLit(masterNode, m_PassForwardOnly, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Experimental.Rendering.HDPipeline.EyeGUI""");

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
