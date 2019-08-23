﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Morestachio.Framework;
using Morestachio.Helper;

namespace Morestachio
{
	/// <summary>
	///		The Compiled template
	/// </summary>
	public class MorestachioDocumentResult
	{
		/// <summary>
		///		The Result of the CreateAsync call
		/// </summary>
		public Stream Stream { get; set; }
	}

	/// <summary>
	///     Provided when parsing a template and getting information about the embedded variables.
	/// </summary>
	public class MorestachioDocumentInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MorestachioDocumentInfo"/> class.
		/// </summary>
		/// <param name="options">The options.</param>
		/// <param name="document">The document.</param>
		public MorestachioDocumentInfo([NotNull] ParserOptions options, [NotNull] IDocumentItem document)
			: this(options, document ?? throw new ArgumentNullException(nameof(document)), null)
		{

		}

		internal MorestachioDocumentInfo([NotNull]ParserOptions options, [CanBeNull]IDocumentItem document, [CanBeNull]IEnumerable<IMorestachioError> errors)
		{
			ParserOptions = options ?? throw new ArgumentNullException(nameof(options));
			Document = document;
			Errors = errors ?? Enumerable.Empty<IMorestachioError>();
		}
		
		/// <summary>
		///		The Morestachio Document generated by the <see cref="Parser"/>
		/// </summary>
		[CanBeNull]
		public IDocumentItem Document { get; }

		/// <summary>
		///     The parser Options object that was used to create the Template Delegate
		/// </summary>
		[NotNull]
		public ParserOptions ParserOptions { get; }

		/// <summary>
		///		Gets a list of errors occured while parsing the Template
		/// </summary>
		[NotNull]
		public IEnumerable<IMorestachioError> Errors { get; private set; }

		internal PerformanceProfiler Profiler { get; set; }

		private const int BufferSize = 2024;

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="data"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		[MustUseReturnValue("The Stream contains the template. Use CreateAndStringify() to get the string of it")]
		[NotNull]
		public async Task<MorestachioDocumentResult> CreateAsync([NotNull]object data, CancellationToken token)
		{
			if (Errors.Any())
			{
				throw new AggregateException("You cannot Create this Template as there are one or more Errors. See Inner Exception for more infos.", Errors.Select(e => e.GetException())).Flatten();
			}

			var timeoutCancellation = new CancellationTokenSource();
			if (ParserOptions.Timeout != TimeSpan.Zero)
			{
				timeoutCancellation.CancelAfter(ParserOptions.Timeout);
				var anyCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCancellation.Token);
				token = anyCancellationToken.Token;
			}
			var sourceStream = ParserOptions.SourceFactory();
			try
			{
				if (sourceStream == null)
				{
					throw new NullReferenceException("The created stream is null.");
				}

				if (!sourceStream.CanWrite)
				{
					throw new InvalidOperationException($"The stream '{sourceStream.GetType()}' is ReadOnly.");
				}

				using (var byteCounterStream = new ByteCounterStream(sourceStream,
					ParserOptions.Encoding, BufferSize, true))
				{
					var context = new ContextObject(ParserOptions, "", null)
					{
						Value = data,
						CancellationToken = token
					};

					await MorestachioDocument.ProcessItemsAndChildren(new[] { Document }, byteCounterStream,
						context, new ScopeData());
				}

				if (timeoutCancellation.IsCancellationRequested)
				{
					sourceStream.Dispose();
					throw new TimeoutException($"The requested timeout of '{ParserOptions.Timeout:g}' for report generation was reached.");
				}
			}
			catch(Exception ex)
			{
				//If there is any exception while generating the template we must dispose any data written to the stream as it will never returned and might 
				//create a memory leak with this. This is also true for a timeout
				sourceStream?.Dispose();
				throw;
			}
			return new MorestachioDocumentResult()
			{
				Stream = sourceStream
			};
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[MustUseReturnValue("The Stream contains the template. Use CreateAndStringify() to get the string of it")]
		[NotNull]
		public async Task<MorestachioDocumentResult> CreateAsync([NotNull]object data)
		{
			return await CreateAsync(data, CancellationToken.None);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		[NotNull]
		public async Task<string> CreateAndStringifyAsync([NotNull]object source)
		{
			return await CreateAndStringifyAsync(source, CancellationToken.None);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		[NotNull]
		public async Task<string> CreateAndStringifyAsync([NotNull]object source, CancellationToken token)
		{
			return (await CreateAsync(source, token)).Stream.Stringify(true, ParserOptions.Encoding);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		[MustUseReturnValue("The Stream contains the template. Use CreateAndStringify() to get the string of it")]
		[NotNull]
		public MorestachioDocumentResult Create([NotNull]object source, CancellationToken token)
		{
			MorestachioDocumentResult result = null;
			using (var async = AsyncHelper.Wait)
			{
				async.Run(CreateAsync(source, token), e => result = e);
			}

			return result;
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		[MustUseReturnValue("The Stream contains the template. Use CreateAndStringify() to get the string of it")]
		[NotNull]
		public MorestachioDocumentResult Create([NotNull]object source)
		{
			return Create(source, CancellationToken.None);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		[NotNull]
		public string CreateAndStringify([NotNull]object source)
		{
			return CreateAndStringify(source, CancellationToken.None);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		[NotNull]
		public string CreateAndStringify([NotNull]object source, CancellationToken token)
		{
			return Create(source, token).Stream.Stringify(true, ParserOptions.Encoding);
		}
	}
}