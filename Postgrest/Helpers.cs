﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Postgrest.Exceptions;
using Postgrest.Models;
using Postgrest.Responses;
using Supabase.Core;
using Supabase.Core.Extensions;
[assembly: InternalsVisibleTo("PostgrestTests")]

namespace Postgrest
{

	internal static class Helpers
	{
		private static readonly HttpClient Client = new HttpClient();

		/// <summary>
		/// Helper to make a request using the defined parameters to an API Endpoint and coerce into a model. 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="clientOptions"></param>
		/// <param name="method"></param>
		/// <param name="url"></param>
		/// <param name="data"></param>
		/// <param name="headers"></param>
		/// <param name="serializerSettings"></param>
		/// <param name="getHeaders"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static async Task<ModeledResponse<T>> MakeRequest<T>(ClientOptions clientOptions, HttpMethod method, string url, JsonSerializerSettings serializerSettings, object? data = null,
			Dictionary<string, string>? headers = null, Func<Dictionary<string, string>>? getHeaders = null, CancellationToken cancellationToken = default) where T : BaseModel, new()
		{
			var baseResponse = await MakeRequest(clientOptions, method, url, serializerSettings, data, headers, cancellationToken);
			return new ModeledResponse<T>(baseResponse, serializerSettings, getHeaders);
		}

		/// <summary>
		/// Helper to make a request using the defined parameters to an API Endpoint.
		/// </summary>
		/// <param name="clientOptions"></param>
		/// <param name="method"></param>
		/// <param name="url"></param>
		/// <param name="data"></param>
		/// <param name="headers"></param>
		/// <param name="serializerSettings"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static async Task<BaseResponse> MakeRequest(ClientOptions clientOptions, HttpMethod method, string url, JsonSerializerSettings serializerSettings, object? data = null,
			Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
		{
			var builder = new UriBuilder(url);
			var query = HttpUtility.ParseQueryString(builder.Query);

			if (data != null && method == HttpMethod.Get)
			{
				// Case if it's a Get request the data object is a dictionary<string,string>
				if (data is Dictionary<string, string> reqParams)
				{
					foreach (var param in reqParams)
						query[param.Key] = param.Value;
				}
			}

			builder.Query = query.ToString();

			using var requestMessage = new HttpRequestMessage(method, builder.Uri);

			if (data != null && method != HttpMethod.Get)
			{
				var stringContent = JsonConvert.SerializeObject(data, serializerSettings);

				if (!string.IsNullOrWhiteSpace(stringContent) && JToken.Parse(stringContent).HasValues)
				{
					requestMessage.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");
				}
			}

			if (headers != null)
			{
				foreach (var kvp in headers)
				{
					requestMessage.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
				}
			}

			var response = await Client.SendAsync(requestMessage, cancellationToken);
			var content = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
				return new BaseResponse(clientOptions, response, content);

			var exception = new PostgrestException(content)
			{
				Content = content,
				Response = response,
				StatusCode = (int)response.StatusCode
			};
			exception.AddReason();
			throw exception;
		}

		/// <summary>
		/// Prepares the request with appropriate HTTP headers expected by Postgrest.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="headers"></param>
		/// <param name="options"></param>
		/// <param name="rangeFrom"></param>
		/// <param name="rangeTo"></param>
		/// <returns></returns>
		public static Dictionary<string, string> PrepareRequestHeaders(HttpMethod method, Dictionary<string, string>? headers = null, ClientOptions? options = null, int rangeFrom = int.MinValue, int rangeTo = int.MinValue)
		{
			options ??= new ClientOptions();

			headers = headers == null ? new Dictionary<string, string>(options.Headers) : options.Headers.MergeLeft(headers);

			if (!string.IsNullOrEmpty(options.Schema))
			{
				headers.Add(method == HttpMethod.Get ? "Accept-Profile" : "Content-Profile", options.Schema);
			}

			if (rangeFrom != int.MinValue)
			{
				var formatRangeTo = rangeTo != int.MinValue ? rangeTo.ToString() : null;

				headers.Add("Range-Unit", "items");
				headers.Add("Range", $"{rangeFrom}-{formatRangeTo}");
			}

			if (!headers.ContainsKey("X-Client-Info"))
			{
				headers.Add("X-Client-Info", Util.GetAssemblyVersion(typeof(Client)));
			}

			return headers;
		}
	}
}
