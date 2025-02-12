﻿using UnityEngine;

namespace UnityGLTF
{
    class MetalRoughMap : MetalRough2StandardMap
    {
        public MetalRoughMap(int MaxLOD = 1000) : base("DCL/LWRP/Lit", MaxLOD) { }
        public MetalRoughMap(string shaderName, int MaxLOD = 1000) : base(shaderName, MaxLOD) { }
        public MetalRoughMap(Material m, int MaxLOD = 1000) : base(m, MaxLOD) { }

        public override int NormalTexCoord
        {
            get { return 0; }
            set { return; }
        }

        public override int OcclusionTexCoord
        {
            get { return 0; }
            set { return; }
        }

        public override int EmissiveTexCoord
        {
            get { return 0; }
            set { return; }
        }

        public override int BaseColorTexCoord
        {
            get { return 0; }
            set { return; }
        }

        public override Texture MetallicRoughnessTexture
        {
            get { return _material.GetTexture("_MetallicGlossMap"); }
            set
            {
                _material.SetTexture("_MetallicGlossMap", value);
                _material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
        }

        public override int MetallicRoughnessTexCoord
        {
            get { return 0; }
            set { return; }
        }

        public override IUniformMap Clone()
        {
            var copy = new MetalRoughMap(new Material(_material));
            base.Copy(copy);
            return copy;
        }
    }
}
