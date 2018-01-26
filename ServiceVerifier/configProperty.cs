using Newtonsoft.Json;
using System;

namespace ServiceVerifier
{
    public class ConfigProperty
    {
        [JsonProperty(PropertyName = "serviceName", Required = Required.Always)]
        public string ServiceName { get; set; }
        [JsonProperty(PropertyName = "sentryDns", Required = Required.Always)]
        public string SentryDns { get; set; }
        [JsonProperty(PropertyName = "emailHost", Required = Required.Always)]
        public string EmailHost { get; set; }
        [JsonProperty(PropertyName = "emailFrom", Required = Required.Always)]
        public string EmailFrom { get; set; }
        [JsonProperty(PropertyName = "emailName", Required = Required.Always)]
        public string EmailName { get; set; }
        [JsonProperty(PropertyName = "emailPassword", Required = Required.Always)]
        public string EmailPassword { get; set; }
        [JsonProperty(PropertyName = "emailTo", Required = Required.Always)]
        public string EmailTo { get; set; }
    }
}
