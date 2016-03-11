﻿using Hl7.Fhir.Model;
using Microsoft.Practices.Unity;
using Spark.Configuration;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.ValueProviders;
using System.Web.Http.ValueProviders.Providers;
using Spark.Infrastructure;

namespace Spark.Controllers
{
    [RoutePrefix("fhir"), EnableCors("*", "*", "*", "*")]
    [RouteDataValuesOnly]
    public class FhirController : ApiController
    {
        FhirServiceOld fhirServiceOld;

        [InjectionConstructor]
        public FhirController(FhirServiceOld fhirServiceOld)
        {
            // This will be a (injected) constructor parameter in ASP.vNext.
            this.fhirServiceOld = fhirServiceOld;
        }

        [HttpGet, Route("{type}/{id}")]
        public FhirResponse Read(string type, string id)
        {
            ConditionalHeaderParameters parameters = new ConditionalHeaderParameters(Request);
            Key key = Key.Create(type, id);
            FhirResponse response = fhirServiceOld.Read(key, parameters);

            return response;
        }

        [HttpGet, Route("{type}/{id}/_history/{vid}")]
        public FhirResponse VRead(string type, string id, string vid)
        {
            Key key = Key.Create(type, id, vid);
            return fhirServiceOld.VersionRead(key);
        }

        [HttpPut, Route("{type}/{id}")]
        public FhirResponse Update(string type, string id, Resource resource)
        {
            string versionid = Request.IfMatchVersionId();
            Key key = Key.Create(type, id, versionid);
            return fhirServiceOld.Update(key, resource);
        }

        [HttpPost, Route("{type}")]
        public FhirResponse Create(string type, Resource resource)
        {
            //entry.Tags = Request.GetFhirTags(); // todo: move to model binder?
            Key key = Key.Create(type);
            return fhirServiceOld.Create(key, resource);
        }

        [HttpDelete, Route("{type}/{id}")]
        public FhirResponse Delete(string type, string id)
        {
            Key key = Key.Create(type, id);
            FhirResponse response = fhirServiceOld.Delete(key);
            return response;
        }

        [HttpDelete, Route("{type}")]
        public FhirResponse ConditionalDelete(string type)
        {
            Key key = Key.Create(type);
            return fhirServiceOld.ConditionalDelete(key, Request.TupledParameters());
        }

        [HttpGet, Route("{type}/{id}/_history")]
        public FhirResponse History(string type, string id)
        {
            Key key = Key.Create(type, id);
            var parameters = new HistoryParameters(Request);
            return fhirServiceOld.History(key, parameters);
        }

        // ============= Validate
        [HttpPost, Route("{type}/{id}/$validate")]
        public FhirResponse Validate(string type, string id, Resource resource)
        {
            //entry.Tags = Request.GetFhirTags();
            Key key = Key.Create(type, id);
            return fhirServiceOld.ValidateOperation(key, resource);
        }

        [HttpPost, Route("{type}/$validate")]
        public FhirResponse Validate(string type, Resource resource)
        {
            // DSTU2: tags
            //entry.Tags = Request.GetFhirTags();
            Key key = Key.Create(type);
            return fhirServiceOld.ValidateOperation(key, resource);
        }

        // ============= Type Level Interactions

        [HttpGet, Route("{type}")]
        public FhirResponse Search(string type)
        {
            var searchparams = Request.GetSearchParams();
            //int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            //string sortby = Request.GetParameter(FhirParameter.SORT);

            return fhirServiceOld.Search(type, searchparams);
        }

        [HttpPost, HttpGet, Route("{type}/_search")]
        public FhirResponse SearchWithOperator(string type)
        {
            // todo: get tupled parameters from post.
            return Search(type);
        }

        [HttpGet, Route("{type}/_history")]
        public FhirResponse History(string type)
        {
            var parameters = new HistoryParameters(Request);
            return fhirServiceOld.History(type, parameters);
        }

        // ============= Whole System Interactions

        [HttpGet, Route("metadata")]
        public FhirResponse Metadata()
        {
            return Respond.WithResource(Factory.GetSparkConformance());
        }

        [HttpOptions, Route("")]
        public FhirResponse Options()
        {
            return Respond.WithResource(Factory.GetSparkConformance());
        }

        [HttpPost, Route("")]
        public FhirResponse Transaction(Bundle bundle)
        {
            return fhirServiceOld.Transaction(bundle);
        }

        //[HttpPost, Route("Mailbox")]
        //public FhirResponse Mailbox(Bundle document)
        //{
        //    Binary b = Request.GetBody();
        //    return service.Mailbox(document, b);
        //}

        [HttpGet, Route("_history")]
        public FhirResponse History()
        {
            var parameters = new HistoryParameters(Request);
            return fhirServiceOld.History(parameters);
        }

        [HttpGet, Route("_snapshot")]
        public FhirResponse Snapshot()
        {
            string snapshot = Request.GetParameter(FhirParameter.SNAPSHOT_ID);
            int start = Request.GetIntParameter(FhirParameter.SNAPSHOT_INDEX) ?? 0;
            return fhirServiceOld.GetPage(snapshot, start);
        }

        // Operations

        [HttpPost, Route("${operation}")]
        public FhirResponse ServerOperation(string operation)
        {
            switch (operation.ToLower())
            {
                case "error": throw new Exception("This error is for testing purposes");
                default: return Respond.WithError(HttpStatusCode.NotFound, "Unknown operation");
            }
        }

        [HttpPost, Route("{type}/{id}/${operation}")]
        public FhirResponse InstanceOperation(string type, string id, string operation, Parameters parameters)
        {
            Key key = Key.Create(type, id);
            switch (operation.ToLower())
            {
                case "meta": return fhirServiceOld.ReadMeta(key);
                case "meta-add": return fhirServiceOld.AddMeta(key, parameters);
                case "meta-delete":
                case "document":
                case "$everything": // patient

                default: return Respond.WithError(HttpStatusCode.NotFound, "Unknown operation");
            }
        }

        // ============= Tag Interactions

        /*
        [HttpGet, Route("_tags")]
        public TagList AllTags()
        {
            return service.TagsFromServer();
        }

        [HttpGet, Route("{type}/_tags")]
        public TagList ResourceTags(string type)
        {
            return service.TagsFromResource(type);
        }

        [HttpGet, Route("{type}/{id}/_tags")]
        public TagList InstanceTags(string type, string id)
        {
            return service.TagsFromInstance(type, id);
        }

        [HttpGet, Route("{type}/{id}/_history/{vid}/_tags")]
        public HttpResponseMessage HistoryTags(string type, string id, string vid)
        {
            TagList tags = service.TagsFromHistory(type, id, vid);
            return Request.CreateResponse(HttpStatusCode.OK, tags);
        }

        [HttpPost, Route("{type}/{id}/_tags")]
        public HttpResponseMessage AffixTag(string type, string id, TagList taglist)
        {
            service.AffixTags(type, id, taglist != null ? taglist.Category : null);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost, Route("{type}/{id}/_history/{vid}/_tags")]
        public HttpResponseMessage AffixTag(string type, string id, string vid, TagList taglist)
        {
            service.AffixTags(type, id, vid, taglist != null ? taglist.Category : null);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost, Route("{type}/{id}/_tags/_delete")]
        public HttpResponseMessage DeleteTags(string type, string id, TagList taglist)
        {
            service.RemoveTags(type, id, taglist != null ? taglist.Category : null);
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        [HttpPost, Route("{type}/{id}/_history/{vid}/_tags/_delete")]
        public HttpResponseMessage DeleteTags(string type, string id, string vid, TagList taglist)
        {
            service.RemoveTags(type, id, vid, taglist != null ? taglist.Category : null);
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }
        */

    }

}
