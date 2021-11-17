#version 330 core
out vec4 FragColor;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoords;

uniform sampler2D texture_diffuse1;
uniform sampler2D texture_specular1;
uniform sampler2D texture_normal1;

uniform vec3 LightColor;
uniform vec3 ViewPos;
uniform vec3 LightDirection;

void main()
{    
    //ambient
    vec3 ambient = LightColor * 0.2f * vec3(texture(texture_diffuse1, TexCoords));

    //diffuse
    float diff = max(dot(vec3(texture(texture_normal1, TexCoords)), LightDirection), 0.0);
    vec3 diffuse = 0.8f * diff * vec3(texture(texture_diffuse1, TexCoords));

    // specular
    vec3 viewDir = normalize(ViewPos - FragPos);
    vec3 reflectDir = reflect(-LightDirection, Normal);  
    float spec = pow(max(dot(viewDir, normalize(reflectDir)), 0.0), 64);
    vec3 specular = 1.2f * spec * vec3(texture(texture_specular1, TexCoords));  

    FragColor = vec4(ambient + diffuse + specular, 1.0f);
}