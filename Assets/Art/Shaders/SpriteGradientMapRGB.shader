//Only use for sprites, use vertex color as gradient opacity
Shader "Game/Sprite/GradientMapRGB"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_GradientRTex("Gradient for Red Channel (RGB)", 2D) = "white" {}
		_GradientGTex("Gradient for Green Channel (RGB)", 2D) = "white" {}
		_GradientBTex("Gradient for Blue Channel (RGB)", 2D) = "white" {}
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
		[HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0

		_ColorOverlay("Overlay", Color) = (0,0,0,0)
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex SpriteVert
			#pragma fragment SpriteFrag_ColorGradients
			#pragma target 2.0
			#pragma multi_compile_instancing
			#pragma multi_compile_local _ PIXELSNAP_ON
			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
			#include "UnitySprites.cginc"

			sampler2D _GradientRTex;
			sampler2D _GradientGTex;
			sampler2D _GradientBTex;

			fixed4 SpriteFrag_ColorGradients(v2f IN) : SV_Target
			{	
				fixed4 c = SampleSpriteTexture(IN.texcoord);// *IN.color;
				
				fixed3 gradientR = tex2D(_GradientRTex, fixed2(c.r, 0.5));
				fixed3 gradientG = tex2D(_GradientGTex, fixed2(c.g, 0.5));
				fixed3 gradientB = tex2D(_GradientBTex, fixed2(c.b, 0.5));

				fixed4 fc = fixed4(
					gradientR.rgb * IN.color.r + gradientG.rgb * IN.color.g + gradientB.rgb * IN.color.b,
					c.a * IN.color.a
				);

				fc.rgb *= fc.a;
				return fc;
			}
		ENDCG
		}
	}
}