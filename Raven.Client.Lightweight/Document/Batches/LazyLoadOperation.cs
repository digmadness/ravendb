﻿using System;
using System.Net;
using Raven.Abstractions.Data;
using Raven.Client.Connection;
using Raven.Client.Document.SessionOperations;
using Raven.Client.Shard;
#if SILVERLIGHT || NETFX_CORE
using Raven.Client.Silverlight.MissingFromSilverlight;
#else
using System.Collections.Specialized;
#endif
using Raven.Json.Linq;
using System.Linq;

namespace Raven.Client.Document.Batches
{
	public class LazyLoadOperation<T> : ILazyOperation
	{
		private readonly string key;
		private readonly LoadOperation loadOperation;
		private readonly Action<RavenJObject> handleInternalMetadata;

		public LazyLoadOperation(string key, LoadOperation loadOperation, Action<RavenJObject> handleInternalMetadata)
		{
			this.key = key;
			this.loadOperation = loadOperation;
			this.handleInternalMetadata = handleInternalMetadata;
		}

		public GetRequest CreateRequest()
		{
			string path = "/docs/" + Uri.EscapeDataString(key);

			return new GetRequest {Url = path};
		}

		public object Result { get; set; }

		public bool RequiresRetry { get; set; }

#if !SILVERLIGHT
		public void HandleResponses(GetResponse[] responses, ShardStrategy shardStrategy)
		{
			var response = responses.OrderBy(x => x.Status).First(); // this way, 200 response is higher than 404
			HandleResponse(response);
		}
#endif

		public void HandleResponse(GetResponse response)
		{
			if(response.Status == 404)
			{
				Result = null;
				RequiresRetry = false;
				return;
			}

			var headers = new NameValueCollection();
			foreach (var header in response.Headers)
			{
				headers[header.Key] = header.Value;
			}
			var jsonDocument = SerializationHelper.DeserializeJsonDocument(key, response.Result, headers, (HttpStatusCode)response.Status);
			HandleResponse(jsonDocument);
		}

		private void HandleResponse(JsonDocument jsonDocument)
		{
			RequiresRetry = loadOperation.SetResult(jsonDocument);
			if (RequiresRetry == false)
				Result = loadOperation.Complete<T>();
		}

		public IDisposable EnterContext()
		{
			return loadOperation.EnterLoadContext();
		}

#if !SILVERLIGHT
		public object ExecuteEmbedded(IDatabaseCommands commands)
		{
			return commands.Get(key);
		}
#endif

		public void HandleEmbeddedResponse(object result)
		{
			var multiLoadResult = result as MultiLoadResult;
			if (multiLoadResult != null)
			{
				var resultItem = multiLoadResult.Results.FirstOrDefault();
				var ravenJObject = resultItem.Value<RavenJArray>("$values")
				                             .Cast<RavenJObject>()
											 .Select(value =>
											 {
												 if (handleInternalMetadata != null)
													 handleInternalMetadata(value);
												 return value;
											 })
				                             .FirstOrDefault();
				var jsonDocument = SerializationHelper.RavenJObjectToJsonDocument(ravenJObject);
				HandleResponse(jsonDocument);
				return;
			}

			HandleResponse((JsonDocument) result);
		}
	}
}