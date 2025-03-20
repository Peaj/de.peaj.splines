// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Debug/Normals" {
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass{
			Tags{ "LightMode" = "Always" }

			Fog{ Mode Off }
			ZWrite On
			ZTest LEqual
			Cull Back
			Lighting Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = v.normal;
				return o;
			}

			fixed4 frag(v2f i) : COLOR{
				return half4(i.normal,1.0);
			}
			ENDCG
		}
	}
}