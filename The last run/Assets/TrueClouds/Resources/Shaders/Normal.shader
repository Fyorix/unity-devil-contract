Shader "Hidden/Clouds/Normal"
{
    Properties
    {
    }

    SubShader
    {
        Tags{ "RenderType"="Transparent" "Queue"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite on
        Cull Back
        Lighting Off
        Fog{ Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                float diff = saturate(dot(n, lightDir));
                diff = pow(diff, 1.5);

                float brightness = lerp(0.6, 1.0, diff);

                // Effet lumière derrière (scattering)
                float scatter = pow(saturate(dot(-lightDir, n)), 3.0);

                float3 color = float3(brightness, brightness, brightness);
                color += scatter * 1.5;

                float alpha = lerp(0.3, 0.6, diff);
                alpha -= scatter * 0.2;
                alpha = saturate(alpha);

                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
