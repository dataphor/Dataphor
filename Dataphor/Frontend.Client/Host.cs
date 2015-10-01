/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Net;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client
{
	[ListInDesigner(false)]
	public class Host : Node, IHost
	{
		/// <summary> Create a new host object. </summary>
		/// <param name="session"> The session which this host is part of. </param>
		public Host(Session session) : base()
		{
			_session = session;
		}

		/// <remarks> Calls Close (<see cref="Close"/>) and dereferences ParentHost. </remarks>
		protected override void Dispose(bool disposing)
		{
			try
			{
				Close();
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		// Session

		private Session _session;
		public virtual Session Session
		{
			get { return _session; }
		}

		// Pipe

		public virtual Pipe Pipe
		{
			get { return _session.Pipe; }
		}

		// Document

		private string _document = String.Empty;
		public string Document
		{
			get { return _document; }
			set 
			{ 
				if (value != _document)
				{
					_document = ( value == null ? String.Empty : value ); 
					if (OnDocumentChanged != null)
						OnDocumentChanged(this, EventArgs.Empty);
				}
			}
		}

		public event EventHandler OnDocumentChanged;

		// NextRequest

		private Request _nextRequest;
		public Request NextRequest
		{
			get { return _nextRequest; }
			set { _nextRequest = value; }
		}

		/// <summary> Ensures that this host does not have a parent. </summary>
		protected void CheckRootNode()
		{
			if (Parent != null)
				throw new ClientException(ClientException.Codes.NotRootNode);
		}

		// Active

		private bool _active;
		/// <remarks> When true the host node and all it's children are active. </remarks>
		public override bool Active
		{
			get
			{
				if (Parent != null)
					return base.Active;
				else
					return _active & !Transitional;
			}
		}

		public void Open()
		{
			Open(false);
		}

		public void Open(bool deferAfterActivate)
		{
			if (!Active)
			{
				CheckRootNode();
				ActivateAll();
				_active = true;
				if (!deferAfterActivate)
					AfterActivate();
			}
		}

		/// <summary> AfterOpen is to be called only if the host was opened with ADeferAfterActivate = true. </summary>
		public void AfterOpen()
		{
			AfterActivate();
		}
		
		public void Close()
		{
			if (Active)
			{
				CheckRootNode();
				try
				{
					BeforeDeactivate();
				}
				finally
				{
					_active = false;
					DeactivateAll();
				}
			}
		}

		public INode Load(string document, object instance)
		{
			string documentValue = EvaluateDocument(document);
			Children.Clear();
 
			Deserializer deserializer;

			deserializer = Session.CreateDeserializer();

			INode node;
			try
			{
				node = (INode)deserializer.Deserialize(documentValue, instance);
				try
				{
					node.Owner = this;
				}
				catch
				{
					node.Dispose();
					throw;
				}
			}
			catch (Exception E)
			{
				throw new ClientException(ClientException.Codes.DocumentDeserializationError, E, documentValue);
			}
			if (deserializer.Errors.Count > 0)
				HandleDeserializationErrors(deserializer.Errors);
			Children.Add(node);
			Document = document;
			return node;
		}

		private string EvaluateDocument(string document)
		{
			// Optimization: check to see if the document expression is merely a string literal before making a trip to the server
			var expression = new DAE.Language.D4.Parser().ParseExpression(document);
			var stringValue = expression as DAE.Language.ValueExpression;
			if (stringValue != null && stringValue.Token == DAE.Language.TokenType.String)
				return (string)stringValue.Value;
			else
				using (DAE.Runtime.Data.IScalar scalar = Pipe.RequestDocument(document))
				{
					return scalar.AsString;
				}
		}

		public INode LoadNext(object instance)
		{
			Request request = NextRequest;
			NextRequest = null;
			if (request != null)
				return Load(request.Document, instance);
			else
				return null;
		}

		/// <summary> Determines a unique name for the specified node. </summary>
		public void GetUniqueName(INode node)
		{
            string baseName;
			int count = 1;

			if (node.Name == String.Empty)
				baseName = node.GetType().Name;
			else
				baseName = node.Name;

			if (GetNode(baseName, node) == null)	// will the unaffected name do?
				node.Name = baseName;
			else
			{
				// Strip any trailing number from the name
				int numIndex = baseName.Length - 1;
				while ((numIndex >= 0) && Char.IsNumber(baseName, numIndex))
					numIndex--;
				if (numIndex < (baseName.Length - 1))
				{
					count = Int32.Parse(baseName.Substring(numIndex + 1));
					baseName = baseName.Substring(0, numIndex + 1);
				}

				// Iterate through numbers until a unique number is found
				string name;
				do
				{
					name = baseName + count.ToString();
					count++;
				} while (GetNode(name, node) != null);
				node.Name = name;
			}
		}

		/// <remarks> Allows any type of child. </remarks>
		public override bool IsValidChild(Type childType)
		{
			return true;
		}

		public event DeserializationErrorsHandler OnDeserializationErrors;

		public void HandleDeserializationErrors(ErrorList errors)
		{
			if (OnDeserializationErrors != null)
				OnDeserializationErrors(this, errors);
		}
	}

	public delegate void DeserializationErrorsHandler(IHost AHost, ErrorList AErrors);
}