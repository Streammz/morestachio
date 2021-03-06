﻿using System;
using System.Collections.Generic;
using System.Text;
using Morestachio.Document.Contracts;
using Morestachio.Framework;
using Morestachio.ParserErrors;

namespace Morestachio.Document.Custom
{
	/// <summary>
	///		Allows the injection of a custom DocumentItem 
	/// </summary>
	public abstract class CustomDocumentItemProvider
	{
		/// <inheritdoc />
		public CustomDocumentItemProvider()
		{
			ScopeStack = new Stack<Tuple<string, int>>();
		}

		/// <summary>
		///		A Custom stack that keeps track of enclosing tokens such as #IF and /IF
		/// </summary>
		public Stack<Tuple<string, int>> ScopeStack { get; }

		/// <summary>
		///		Should check if the token contains this partial token. If returns true further actions will happen.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public abstract bool ShouldTokenize(string token);

		/// <summary>
		///		An helper object that will be given to the Tokenize method
		/// </summary>
		public class TokenInfo
		{
			private readonly List<int> _lines;
			private readonly int _tokenIndex;

			internal TokenInfo(string token,
				List<int> lines,
				int tokenIndex)
			{
				_lines = lines;
				_tokenIndex = tokenIndex;
				Token = token;
				Errors = new List<IMorestachioError>();
			}

			/// <summary>
			///		The obtained Token
			/// </summary>
			public string Token { get; }

			/// <summary>
			///		Can be filled to return errors that occured in the formatting process
			/// </summary>
			public ICollection<IMorestachioError> Errors { get; }


			/// <summary>
			///		Can be used to format parts or a whole portion of the path
			/// </summary>
			/// <param name="format"></param>
			/// <param name="options"></param>
			/// <returns></returns>
			public IEnumerable<TokenPair> Format(string format, ParserOptions options)
			{
				var enumerateFormats = Tokenizer.EnumerateFormats(format, _lines, _tokenIndex, Errors);
				var tokenizeFormattables = Tokenizer.TokenizeFormattables(enumerateFormats, format, format, _lines, _tokenIndex, Errors, options);
				return tokenizeFormattables;
			}
		}

		/// <summary>
		///		Should return any kind of token Pair that encapsulates the value for the DocumentItem
		/// </summary>
		/// <param name="token"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public abstract IEnumerable<TokenPair> Tokenize(TokenInfo token, ParserOptions options);

		/// <summary>
		///		Should return True if the Token is produced by this provider and should be parsed with this provider
		/// </summary>
		/// <param name="token"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public abstract bool ShouldParse(TokenPair token, ParserOptions options);

		/// <summary>
		///		Should return an document item that will be invoked when parsing the Template
		/// </summary>
		/// <param name="token"></param>
		/// <param name="options"></param>
		/// <param name="buildStack"></param>
		/// <returns></returns>
		public abstract IDocumentItem Parse(TokenPair token, ParserOptions options, Stack<DocumentScope> buildStack);
	}
}
