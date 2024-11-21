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
    /// HasDataSpecification
    /// </summary>
    [DataContract]
    public partial class HasDataSpecification : IEquatable<HasDataSpecification>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HasDataSpecification" /> class.
        /// </summary>
        /// <param name="embeddedDataSpecifications">embeddedDataSpecifications.</param>
        public HasDataSpecification(List<EmbeddedDataSpecification> embeddedDataSpecifications = default(List<EmbeddedDataSpecification>))
        {
            this.EmbeddedDataSpecifications = embeddedDataSpecifications;
        }

        /// <summary>
        /// Gets or Sets EmbeddedDataSpecifications
        /// </summary>
        [DataMember(Name = "embeddedDataSpecifications", EmitDefaultValue = false)]
        public List<EmbeddedDataSpecification> EmbeddedDataSpecifications { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class HasDataSpecification {\n");
            sb.Append("  EmbeddedDataSpecifications: ").Append(EmbeddedDataSpecifications).Append("\n");
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
            return this.Equals(input as HasDataSpecification);
        }

        /// <summary>
        /// Returns true if HasDataSpecification instances are equal
        /// </summary>
        /// <param name="input">Instance of HasDataSpecification to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(HasDataSpecification input)
        {
            if (input == null)
                return false;

            return
                (
                    this.EmbeddedDataSpecifications == input.EmbeddedDataSpecifications ||
                    this.EmbeddedDataSpecifications != null &&
                    input.EmbeddedDataSpecifications != null &&
                    this.EmbeddedDataSpecifications.SequenceEqual(input.EmbeddedDataSpecifications)
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
                if (this.EmbeddedDataSpecifications != null)
                    hashCode = hashCode * 59 + this.EmbeddedDataSpecifications.GetHashCode();
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
