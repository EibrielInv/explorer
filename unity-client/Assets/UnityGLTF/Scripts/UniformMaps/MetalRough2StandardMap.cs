﻿using UnityEngine;

namespace UnityGLTF
{
    class MetalRough2StandardMap : StandardMap, IMetalRoughUniformMap
    {
        public MetalRough2StandardMap(int MaxLOD = 1000) : base("Standard", MaxLOD) { }
        protected MetalRough2StandardMap(string shaderName, int MaxLOD = 1000) : base(shaderName, MaxLOD) { }
        protected MetalRough2StandardMap(Material m, int MaxLOD = 1000) : base(m, MaxLOD) { }

        public virtual Texture BaseColorTexture
        {
            get { return _material.GetTexture(_BaseMap); }
            set { _material.SetTexture(_BaseMap, value); }
        }

        // not implemented by the Standard shader
        public virtual int BaseColorTexCoord
        {
            get { return 0; }
            set { return; }
        }

        public virtual Color BaseColorFactor
        {
            get { return _material.GetColor(_BaseColor); }
            set { _material.SetColor(_BaseColor, value); }
        }

        public virtual Texture MetallicRoughnessTexture
        {
            get { return null; }
            set
            {
                // cap metalness at 0.5 to compensate for lack of texture
                MetallicFactor = Mathf.Min(0.5f, (float)MetallicFactor);
            }
        }

        // not implemented by the Standard shader
        public virtual int MetallicRoughnessTexCoord
        {
            get { return 0; }
            set { return; }
        }

        public virtual double MetallicFactor
        {
            get { return _material.GetFloat(_Metallic); }
            set { _material.SetFloat(_Metallic, (float)value); }
        }

        public virtual double RoughnessFactor
        {
            get { return 1f - _material.GetFloat(_Smoothness); }
            set { _material.SetFloat(_Smoothness, 1f - (float)value); }
        }

        public override IUniformMap Clone()
        {
            var copy = new MetalRough2StandardMap(new Material(_material));
            base.Copy(copy);
            return copy;
        }
    }
}
