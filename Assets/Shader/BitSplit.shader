Shader "Hidden/BitSplit"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            sampler2D _MainTex;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).xyz;
                float3 ipert8 = floor(col * 256.0);

                return float4(ipert8 / 256.0, 1);
            }
            ENDCG
        }

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

            sampler2D _MainTex;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).xyz;

                // 4bit
                // float3 ipert16 = floor(col * 65536.0);

                // float3 ipert12 = floor(ipert16 / 16.0);
                // float3 high8 = floor(ipert12 / 16.0) * 16.0;

                // float3 ipert4 = ipert12 - high8;

                // float x = ipert4.x;
                // float y = ipert4.y;

                // 8bit
                // float3 ipert16 = floor(col * 65536.0);
                // float3 ipert8 = floor(ipert16 / 256.0);

                // float3 ipert8low = ipert16 - (ipert8 * 256.0);

                float indexHighp = floor(col.z * 16.0);
                float indexLowp  = (col.z * 16.0) - indexHighp;

                // return float4(frac(col.xy * 256.0), indexHighp / 16.0, indexLowp);
                return float4(frac(col.xy * 256.0), col.z, frac(col.z * 256.0));
            }
            ENDCG
        }
    }
}
