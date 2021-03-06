﻿using Newtonsoft.Json;
using Resonance.Common;
using System;
using System.Collections.Generic;

namespace Resonance.Data.Models
{
    [JsonObject("user")]
    public class User : SettingBase, IDisposable
    {
        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("roles")]
        public IEnumerable<Role> Roles { get; set; }

        public static User FromDynamic(dynamic result)
        {
            return new User
            {
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                EmailAddress = result.EmailAddress,
                Password = result.Password,
                DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.DateAdded),
                DateModified = result.DateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.DateModified),
                Enabled = Convert.ToBoolean(result.Enabled),
                Name = result.Name,
            };
        }

        #region HashCode and Equality Overrides

        public static bool operator !=(User left, User right)
        {
            return !(left == right);
        }

        public static bool operator ==(User left, User right)
        {
            if (left is null)
                return right is null;

            if (right is null)
                return false;

            return left.PropertiesEqual(right, nameof(Id), nameof(Name));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as User);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(Id, Name);
        }

        private bool Equals(User item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides

        public void Dispose()
        {
            EmailAddress = null;
            Name = null;
            Password = null;
            Roles = null;
        }
    }
}