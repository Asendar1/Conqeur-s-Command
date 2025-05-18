Shader "Custom/HealthBarShader"
{
    Properties
    {
        _FillAmount ("Fill Amount", Range(0, 1)) = 1.0
        _FillColor ("Fill Color", Color) = (0, 1, 0, 1)
        _BackColor ("Background Color", Color) = (0.5, 0, 0, 0.5)
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1)
        _BorderWidth ("Border Width", Range(0, 0.1)) = 0.01
    }
    SubShader
    {
        Tags {"Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True"}
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _FillAmount;
            fixed4 _FillColor;
            fixed4 _BackColor;
            fixed4 _BorderColor;
            float _BorderWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Border detection
                bool isBorder = i.uv.x < _BorderWidth ||
                               i.uv.x > (1.0 - _BorderWidth) ||
                               i.uv.y < _BorderWidth ||
                               i.uv.y > (1.0 - _BorderWidth);

                if (isBorder)
                    return _BorderColor;

                // Fill vs background
                if (i.uv.x <= _FillAmount)
                    return _FillColor;
                else
                    return _BackColor;
            }
            ENDCG
        }
    }
}
