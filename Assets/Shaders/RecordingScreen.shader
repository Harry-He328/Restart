Shader "Restart/RecordingScreen"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _EdgeColor ("Edge Color", Color) = (1, 0.36, 0.4, 0.24)
        _EdgeWidth ("Edge Width", Range(0.02, 0.45)) = 0.0375
        _ScanlineStrength ("Scanline Strength", Range(0, 0.1)) = 0.022
        _NoiseStrength ("Noise Strength", Range(0, 0.1)) = 0.018
        _Strength ("Strength", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct Attributes { float4 position : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            float4 _EdgeColor;
            float _EdgeWidth;
            float _Strength;
            float _ScanlineStrength;
            float _NoiseStrength;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 distanceToEdge = min(input.uv, 1.0 - input.uv);
                float edge = 1.0 - smoothstep(0.0, _EdgeWidth, min(distanceToEdge.x, distanceToEdge.y));
                float edgeAlpha = _EdgeColor.a * edge * edge;
                float scanAlpha = _ScanlineStrength * (0.5 + 0.5 * cos(input.position.y * 1.5707963 - _Time.y * 2.0));
                float2 grainPixel = floor(input.position.xy) + floor(_Time.y * 30.0) * float2(37.0, 19.0);
                float grain = frac(sin(dot(grainPixel, float2(12.9898, 78.233))) * 43758.5453);
                float noiseAlpha = grain * _NoiseStrength;
                float alpha = edgeAlpha + scanAlpha + noiseAlpha;
                float3 color = (_EdgeColor.rgb * edgeAlpha + grain.xxx * noiseAlpha) / max(alpha, 0.00001);
                return float4(color, saturate(alpha) * _Strength * input.color.a);
            }
            ENDHLSL
        }
    }
}
