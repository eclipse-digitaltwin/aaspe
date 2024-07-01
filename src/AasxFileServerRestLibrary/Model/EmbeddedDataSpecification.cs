/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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
    /// EmbeddedDataSpecification
    /// </summary>
    [DataContract]
    public partial class EmbeddedDataSpecification : IEquatable<EmbeddedDataSpecification>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedDataSpecification" /> class.
        /// </summary>
        /// <param name="dataSpecification">dataSpecification (required).</param>
        /// <param name="dataSpecificationContent">dataSpecificationContent (required).</param>
        public EmbeddedDataSpecification(Reference dataSpecification = default(Reference), DataSpecificationContent dataSpecificationContent = default(DataSpecificationContent))
        {
            // to ensure "dataSpecification" is required (not null)
            if (dataSpecification == null)
            {
                throw new InvalidDataException("dataSpecification is a required property for EmbeddedDataSpecification and cannot be null");
            }
            else
            {
                this.DataSpecification = dataSpecification;
            }
            // to ensure "dataSpecificationContent" is required (not null)
            if (dataSpecificationContent == null)
            {
                throw new InvalidDataException("dataSpecificationContent is a required property for EmbeddedDataSpecification and cannot be null");
            }
            else
            {
                this.DataSpecificationContent = dataSpecificationContent;
            }
        }

        /// <summary>
        /// Gets or Sets DataSpecification
        /// </summary>
        [DataMember(Name = "dataSpecification", EmitDefaultValue = false)]
        public Reference DataSpecification { get; set; }

        /// <summary>
        /// Gets or Sets DataSpecificationContent
        /// </summary>
        [DataMember(Name = "dataSpecificationContent", EmitDefaultValue = false)]
        public DataSpecificationContent DataSpecificationContent { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class EmbeddedDataSpecification {\n");
            sb.Append("  DataSpecification: ").Append(DataSpecification).Append("\n");
            sb.Append("  DataSpecificationContent: ").Append(DataSpecificationContent).Append("\n");
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
            return this.Equals(input as EmbeddedDataSpecification);
        }

        /// <summary>
        /// Returns true if EmbeddedDataSpecification instances are equal
        /// </summary>
        /// <param name="input">Instance of EmbeddedDataSpecification to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(EmbeddedDataSpecification input)
        {
            if (input == null)
                return false;

            return
                (
                    this.DataSpecification == input.DataSpecification ||
                    (this.DataSpecification != null &&
                    this.DataSpecification.Equals(input.DataSpecification))
                ) &&
                (
                    this.DataSpecificationContent == input.DataSpecificationContent ||
                    (this.DataSpecificationContent != null &&
                    this.DataSpecificationContent.Equals(input.DataSpecificationContent))
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
                if (this.DataSpecification != null)
                    hashCode = hashCode * 59 + this.DataSpecification.GetHashCode();
                if (this.DataSpecificationContent != null)
                    hashCode = hashCode * 59 + this.DataSpecificationContent.GetHashCode();
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
