// =============================================================================
// COVERT CUEING SHADER (QEDLab/Covert)
// =============================================================================
//
// PURPOSE:
// This shader implements the visual component of the covert cueing technique.
// It renders an object with standard diffuse lighting and shadows, then adds
// a subtle, radial brightness boost centered on a specified world-space point.
//
// HOW THE BRIGHTNESS BOOST WORKS:
// For each fragment (pixel) on the object's surface:
//   1. Compute the world-space distance from the fragment to _CenterPosition
//   2. Normalize this distance: tmpDis = (Radius - distance) / Radius
//      - At the center: tmpDis = 1.0 (full effect)
//      - At the edge (distance = Radius): tmpDis = 0.0 (no effect)
//      - Beyond the radius: tmpDis is clamped to 0.0
//   3. Add brightness: col.rgb += 0.095 * tmpDis * _Modulation
//      - 0.095 is the maximum brightness increase (~9.5% boost)
//      - _Modulation is driven by an AnimationCurve in CovertObject.cs,
//        creating a temporal pulse/flicker pattern
//
// PARAMETERS (set per-frame by CovertObject.cs):
//   _CenterPosition — World-space center of the brightness effect
//   _Radius         — Falloff radius in world units (0 = effect disabled)
//   _Modulation     — Temporal scaling factor from AnimationCurve (0 to 1)
//   _MainTex        — Base texture of the object
//   _Color          — Base color tint (multiplied with texture; added for demo visibility)
//
// ADAPTED FROM: Original Covert_S.shader in the Urban Grid Environment
// Experiment project (QEDLab). Preserved with added documentation.
// NOTE: _Color property was added for the standalone demo to allow tinting
// primitive objects with darker colors, making the brightness boost visible.
// =============================================================================

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader"QEDLab/Covert"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Base Color Tint", Color) = (1,1,1,1)
        _CenterPosition("CenterPosition", Vector) = (0,0,0,0)
        _Radius("Radius", float) = 1
        _Modulation("Modulation", float) = 1
    }
    SubShader
    {
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            
            // compile shader into multiple variants, with and without shadows
            // (we don't care about any lightmaps yet, so skip these variants)
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            // shadow helper functions and macros
            #include "AutoLight.cginc"
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                SHADOW_COORDS(2) // put shadows data into TEXCOORD2
                fixed3 diff : COLOR0;
                fixed3 ambient : COLOR1;
                float4 pos : SV_POSITION;
            };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Transform vertex to world space for distance calculation in fragment shader
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.texcoord;
                // Standard diffuse lighting calculation
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0.rgb;
                o.ambient = ShadeSH9(half4(worldNormal, 1));
                // compute shadows data
                TRANSFER_SHADOW(o)
            
                return o;
            }

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _CenterPosition;
            float _Radius;
            float _Modulation;
            float3 tmp;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
                fixed shadow = SHADOW_ATTENUATION(i);
                // darken light's illumination with shadow, keep ambient intact
                fixed3 lighting = i.diff * shadow + i.ambient;
                col.rgb *= lighting;
    
                // === COVERT CUEING EFFECT ===
                // Get the vector between this pixel's world position and the object center
                tmp = i.worldPos.xyz - _CenterPosition.xyz;
                // Convert to a normalized falloff: 1.0 at center, 0.0 at Radius distance
                float tmpDis = ((_Radius - length(tmp)) / _Radius);
                // Clamp the falloff between 0 and 1
                if (tmpDis > 1)
                {
                    tmpDis = 1;
                }
                else if (tmpDis < 0)
                {
                    tmpDis = 0;
                }
                // Apply the covert brightness boost:
                // 0.095 = maximum brightness increase (~9.5%)
                // tmpDis = radial falloff (1 at center, 0 at edge)
                // _Modulation = temporal pulse from AnimationCurve (0 to 1)
                // (1, 1, 1) = white tint (uniform across RGB channels)
                col.rgb += 0.095f * tmpDis * _Modulation * (1, 1, 1);
                // === END COVERT CUEING EFFECT ===
                return col;
            }
            ENDCG
        }
        
        // shadow casting support
        UsePass"Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
