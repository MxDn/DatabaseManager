using System;

using DatabaseInterpreter.Model;

using Newtonsoft.Json;

namespace DatabaseInterpreter.Profile
{
    public class AccountProfileInfo : DatabaseAccountInfo
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string DatabaseType { get; set; }

        [JsonIgnore]
        public string Description
        {
            get
            {
                return $"{(!string.IsNullOrEmpty(this.UserId) ? this.UserId : "Integrated Security")}({this.Server})";
            }
        }
    }
}
