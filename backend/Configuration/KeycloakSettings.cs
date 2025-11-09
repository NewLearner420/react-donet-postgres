namespace backend.Configuration
{
    public class KeycloakSettings
    {
        public string Authority { get; set; } = string.Empty;
        public string ExternalAuthority { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public bool RequireHttpsMetadata { get; set; }
        public bool ValidateAudience { get; set; }
        public bool ValidateIssuer { get; set; }
        public string? MetadataAddress { get; set; }
    }
}