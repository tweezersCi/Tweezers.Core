﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Tweezers.DBConnector;
using Tweezers.Schema.Common;
using Tweezers.Schema.DataHolders.Exceptions;

namespace Tweezers.Schema.DataHolders
{
    public sealed class TweezersObject
    {
        public string _id => CollectionName;

        public string CollectionName { get; set; }

        public TweezersDisplayNames DisplayNames { get; } = new TweezersDisplayNames();

        public string SingularName
        {
            get => DisplayNames.SingularName;
            set => DisplayNames.SingularName = value;
        }

        public string PluralName
        {
            get => DisplayNames.PluralName;
            set => DisplayNames.PluralName = value;
        }

        public string Icon { get; set; }

        public Dictionary<string, TweezersField> Fields { get; set; } = new Dictionary<string, TweezersField>();

        public bool Internal { get; set; } = false;

        public DefaultPermission DefaultPermission { get; set; } = DefaultPermission.View;

        public DateTime LastChanged { get; set; } = DateTime.Now;

        public TweezersValidationResult Validate(JObject obj, bool partial)
        {
            JObject filteredObj = obj.Just(Fields.Keys);

            foreach (string fieldName in Fields.Keys)
            {
                TweezersField field = Fields[fieldName];
                dynamic value = filteredObj[fieldName];
                if (value == null && (partial || !field.FieldProperties.Required))
                {
                    continue;
                }

                TweezersValidationResult validationResultForField = field.Validate(value);
                if (!validationResultForField.Valid)
                {
                    throw new TweezersValidationException(validationResultForField);
                }
            }

            return TweezersValidationResult.Accept();
        }

        public TweezersMultipleResults<JObject> FindInDb(IDatabaseProxy proxy, FindOptions<JObject> findOptions = null,
            bool allFields = false)
        {
            FindOptions<JObject> opts = findOptions ?? FindOptions<JObject>.Default();

            TweezersMultipleResults<JObject> results = proxy.List(CollectionName, opts);

            return TweezersMultipleResults<JObject>.Create(
                results.Items.Select(obj => obj.Just(AllFields(allFields))), results.Count);
        }

        public JObject GetById(IDatabaseProxy proxy, string id, bool allFields = false)
        {
            JObject o = proxy.Get(CollectionName, id);
            string[] fields = AllFields(allFields);
            return o?.Just(fields);
        }

        public JObject Create(IDatabaseProxy proxy, JObject data, string suggestedId = null)
        {
            string id = suggestedId ?? Guid.NewGuid().ToString();
            JObject filteredData = data.Just(Fields.Keys);
            return proxy.Add(CollectionName, id, filteredData).Just(AllFields());
        }

        public JObject Update(IDatabaseProxy proxy, string id, JObject data)
        {
            JObject filteredData = data.Just(Fields.Keys);
            return proxy.Edit(CollectionName, id, filteredData).Just(AllFields());
        }

        public bool Delete(IDatabaseProxy proxy, string id)
        {
            return proxy.Delete(CollectionName, id);
        }

        private string[] AllFields(bool allFields = false)
        {
            return EditableFields(allFields)
                .Concat(new[] {"_id"})
                .ToArray();
        }

        private string[] EditableFields(bool allFields)
        {
            return Fields.Where(f => allFields || !f.Value.FieldProperties.UiIgnore)
                .Select(f => f.Key)
                .ToArray();
        }
    }
}