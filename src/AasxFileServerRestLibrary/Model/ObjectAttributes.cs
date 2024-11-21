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
    /// ObjectAttributes
    /// </summary>
    [DataContract]
    public partial class ObjectAttributes : IEquatable<ObjectAttributes>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectAttributes" /> class.
        /// </summary>
        /// <param name="objectAttribute">objectAttribute.</param>
        public ObjectAttributes(List<Property> objectAttribute = default(List<Property>))
        {
            this.ObjectAttribute = objectAttribute;
        }

        /// <summary>
        /// Gets or Sets ObjectAttribute
        /// </summary>
        [DataMember(Name = "objectAttribute", EmitDefaultValue = false)]
        public List<Property> ObjectAttribute { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class ObjectAttributes {\n");
            sb.Append("  ObjectAttribute: ").Append(ObjectAttribute).Append("\n");
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
            return this.Equals(input as ObjectAttributes);
        }

        /// <summary>
        /// Returns true if ObjectAttributes instances are equal
        /// </summary>
        /// <param name="input">Instance of ObjectAttributes to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(ObjectAttributes input)
        {
            if (input == null)
                return false;

            return
                (
                    this.ObjectAttribute == input.ObjectAttribute ||
                    this.ObjectAttribute != null &&
                    input.ObjectAttribute != null &&
                    this.ObjectAttribute.SequenceEqual(input.ObjectAttribute)
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
                if (this.ObjectAttribute != null)
                    hashCode = hashCode * 59 + this.ObjectAttribute.GetHashCode();
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
