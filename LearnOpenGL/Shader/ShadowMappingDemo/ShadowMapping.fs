#version 330 core
out vec4 FragColor;

in VS_OUT {
    vec3 FragPos;
    vec3 Normal;
    vec2 TexCoords;
    vec4 FragPosLightSpace;
} fs_in;

uniform sampler2D diffuseTexture;
uniform sampler2D shadowMap;

uniform vec3 lightPos;
uniform vec3 viewPos;

float GetBlockerDepth(sampler2D shadowMap, vec3 projCoords, vec2 texelSize)
{
    //PCSS: Get Avg Blocker Depth
    int range = 30;
    float AvgBlckerDepth = 0.0f;
    for(int i = -range; i <= range; i++)
    {
        for(int j = -range; j <= range; j++)
        {
            float ShadowMapDepth = texture(shadowMap, projCoords.xy + vec2(i, j) * texelSize).r; 
            if(ShadowMapDepth < projCoords.z + 0.005f)
            {
                AvgBlckerDepth += ( ShadowMapDepth / ((2 * range + 1) * (2 * range + 1)));
            }
        }
    }

    return AvgBlckerDepth;
}

float ShadowCalculation(vec4 fragPosLightSpace)
{
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    // get closest depth value from light's perspective (using [0,1] range fragPosLight as coords)
    float closestDepth = texture(shadowMap, projCoords.xy).r; 
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;
    // check whether current frag pos is in shadow
    float bias = 0.005;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    float CurrentShadowMapDepth = texture(shadowMap, projCoords.xy).r; 

    if(projCoords.z > 1.0)
    {
        return 0.0f;
    }

    if(CurrentShadowMapDepth > currentDepth + bias)
    {
        return 0.0f;
    }

    //PCSS:: Get Average Blocker Depth
    float AvgBlckerDepth = GetBlockerDepth(shadowMap, projCoords, texelSize);

    //PCSS:: Get Filter Size
    float filter_f;
    filter_f = (currentDepth - AvgBlckerDepth) / AvgBlckerDepth;
    filter_f = filter_f * 30.0f;
    int FilterSize = int(ceil(filter_f));

    //PCSS:: Clamp the FilterSize, if FilterSize is too big, application will crash.
    if(FilterSize <= 1)
    {
        FilterSize = 1;
    }
    else if(FilterSize >= 100)
    {
        FilterSize = 100;
    }

    //Do PCF Function
    float shadow = 0.0;
    for(int x = -FilterSize; x <= FilterSize; ++x)
    {
        for(int y = -FilterSize; y <= FilterSize; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r; 
            shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;        
        }    
    } 
    shadow /= (FilterSize * 2 + 1) * (FilterSize * 2 + 1);

    return shadow;
}

void main()
{           
    vec3 color = texture(diffuseTexture, fs_in.TexCoords).rgb;
    vec3 normal = normalize(fs_in.Normal);
    vec3 lightColor = vec3(0.3);
    // ambient
    vec3 ambient = 0.7 * lightColor;
    // diffuse
    vec3 lightDir = normalize(lightPos - fs_in.FragPos);
    float diff = max(dot(lightDir, normal), 0.0);
    vec3 diffuse = diff * lightColor;
    // specular
    vec3 viewDir = normalize(viewPos - fs_in.FragPos);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = 0.0;
    vec3 halfwayDir = normalize(lightDir + viewDir);  
    spec = pow(max(dot(normal, halfwayDir), 0.0), 64.0);
    vec3 specular = spec * lightColor;    
    // calculate shadow
    float shadow = ShadowCalculation(fs_in.FragPosLightSpace);                      
    vec3 lighting = (ambient + (1.0 - shadow) * (diffuse + specular)) * color * 6.0f;    
    
    FragColor = vec4(lighting, 1.0);
}