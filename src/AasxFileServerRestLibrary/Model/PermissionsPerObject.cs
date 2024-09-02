/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

/* 
 * DotAAS Part 2 | HTTP/REST | Entire Interface Collection
 *
 * The entire interface collection as part of Details of the Asset Administration Shell Part 2
 *
 * OpenAPI spec version: Final-Draft
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SwaggerDateConverter = IO.Swagger.Client.SwaggerDateConverter;

namespace IO.Swagger.Model
{
    /// <summary>
    /// PermissionsPerObject
    /// </summary>
    [DataContract]
    public partial class PermissionsPerObject : IEquatable<PermissionsPerObject>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionsPerObject" /> class.
        /// </summary>
        /// <param name="_object">_object.</param>
        /// <param name="permission">permission.</param>
        /// <param name="targetObjectAttributes">targetObjectAttributes.</param>
        public PermissionsPerObject(Reference _object = default(Reference), List<Permission> permission = default(List<Permission>), ObjectAttributes targetObjectAttributes = default(ObjectAttributes))
        {
            this._Object = _object;
            this.Permission = permission;
            this.TargetObjectAttributes = targetObjectAttributes;
        }

        /// <summary>
        /// Gets or Sets _Object
        /// </summary>
        [DataMember(Name = "object", EmitDefaultValue = false)]
        public Reference _Object { get; set; }

        /// <summary>
        /// Gets or Sets Permission
        /// </summary>
        [DataMember(Name = "permission", EmitDefaultValue = false)]
        public List<Permission> Permission { get; set; }

        /// <summary>
        /// Gets or Sets TargetObjectAttributes
        /// </summary>
        [DataMember(Name = "targetObjectAttributes", EmitDefaultValue = false)]
        public ObjectAttributes TargetObjectAttributes { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class PermissionsPerObject {\n");
            sb.Append("  _Object: ").Append(_Object).Append("\n");
            sb.Append("  Permission: ").Append(Permission).Append("\n");
            sb.Append("  TargetObjectAttributes: ").Append(TargetObjectAttributes).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as PermissionsPerObject);
        }

        /// <summary>
        /// Returns true if PermissionsPerObject instances are equal
        /// </summary>
        /// <param name="input">Instance of PermissionsPerObject to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(PermissionsPerObject input)
        {
            if (input == null)
                return false;

            return
                (
                    this._Object == input._Object ||
                    (this._Object != null &&
                    this._Object.Equals(input._Object))
                ) &&
                (
                    this.Permission == input.Permission ||
                    this.Permission != null &&
                    input.Permission != null &&
                    this.Permission.SequenceEqual(input.Permission)
                ) &&
                (
                    this.TargetObjectAttributes == input.TargetObjectAttributes ||
                    (this.TargetObjectAttributes != null &&
                    this.TargetObjectAttributes.Equals(input.TargetObjectAttributes))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this._Object != null)
                    hashCode = hashCode * 59 + this._Object.GetHashCode();
                if (this.Permission != null)
                    hashCode = hashCode * 59 + this.Permission.GetHashCode();
                if (this.TargetObjectAttributes != null)
                    hashCode = hashCode * 59 + this.TargetObjectAttributes.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}
