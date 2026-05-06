Shader "Custom/TargetIndicator_URP"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _CircleColor ("Circle Color", Color) = (1,1,1,1)
        _ArrowColor ("Arrow Color", Color) = (1,1,0,1)

        _CircleRadius ("Circle Radius", Range(0.1, 0.5)) = 0.25
        _CircleThickness ("Circle Thickness", Range(0.005, 0.1)) = 0.02

        _ArrowLength ("Arrow Length", Range(0, 2)) = 1.0
        _ArrowWidth ("Arrow Width", Range(0.005, 0.2)) = 0.04
        _ArrowHeadSize ("Arrow Head Size", Range(0.02, 0.3)) = 0.1

        _Smoothness ("Edge Smoothness", Range(0.001, 0.02)) = 0.005

        _Aspect ("Aspect Ratio (Width / Height)", Float) = 1.0
    }

    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalRenderPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _CircleColor;
            float4 _ArrowColor;

            float _CircleRadius;
            float _CircleThickness;

            float _ArrowLength;
            float _ArrowWidth;
            float _ArrowHeadSize;

            float _Smoothness;
            float _Aspect;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float sdCircle(float2 p, float r)
            {
                return abs(length(p) - r);
            }

            float sdSegment(float2 p, float2 a, float2 b, float width)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                return length(pa - ba * h) - width;
            }

            float sdTriangle(float2 p, float2 p0, float2 p1, float2 p2)
            {
                float2 e0 = p1 - p0;
                float2 e1 = p2 - p1;
                float2 e2 = p0 - p2;
                float2 v0 = p - p0;
                float2 v1 = p - p1;
                float2 v2 = p - p2;
                float2 pq0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
                float2 pq1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
                float2 pq2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);
                float s = sign(e0.x * e2.y - e0.y * e2.x);
                float2 d = min(min(float2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                                   float2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                                   float2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
                return -sqrt(d.x) * sign(d.y);
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Sample sprite texture (required for SpriteRenderer)
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Center UVs
                float2 uv = i.uv - 0.5;

                // Fix aspect ratio so circle stays round
                uv.x *= _Aspect;

                // --- Circle ---
                float circleDist = sdCircle(uv, _CircleRadius);
                float circle = smoothstep(_CircleThickness + _Smoothness,
                                          _CircleThickness - _Smoothness,
                                          circleDist);

                // --- Arrow (RIGHT direction →) ---
                float2 arrowStart = float2(_CircleRadius, 0);
                float2 arrowEnd   = float2(_CircleRadius + _ArrowLength, 0);

                float arrowShaft = sdSegment(uv, arrowStart, arrowEnd, _ArrowWidth);
                float arrow = smoothstep(_Smoothness, -_Smoothness, arrowShaft);

                // Arrow head
                float headBaseX = _CircleRadius + _ArrowLength;

                float2 headTip   = float2(headBaseX + _ArrowHeadSize, 0);
                float2 headLeft  = float2(headBaseX, -_ArrowHeadSize);
                float2 headRight = float2(headBaseX,  _ArrowHeadSize);

                float headDist = sdTriangle(uv, headTip, headLeft, headRight);
                float arrowHead = smoothstep(_Smoothness, -_Smoothness, headDist);

                float arrowCombined = max(arrow, arrowHead);

                // --- Color ---
                half4 circleCol = _CircleColor * circle;
                half4 arrowCol  = _ArrowColor * arrowCombined;

                half4 finalColor = lerp(circleCol, arrowCol, arrowCombined);
                finalColor.a = max(circle * _CircleColor.a, arrowCombined * _ArrowColor.a);

                // Apply sprite texture (keeps compatibility)
                finalColor *= tex;

                return finalColor;
            }

            ENDHLSL
        }
    }
}
