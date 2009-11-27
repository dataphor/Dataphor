/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace Alphora.Dataphor.BOP
{

    /// <summary>
    ///    IBOP (Inherited Basic Object Persistence) provides services to Diff and Merge the
    ///    differences of two BOP XML documents/streams.
    ///    Diff
    ///      extracts the differences between two BOP XML streams and writes them out to 
    ///      another stream.
    ///    Merge
    ///      combines a base BOP XML document with the changes stored in a Diff stream.
    /// </summary>
    public class Inheritance
    {
		public const string CIBOPNamespacePrefix = "ibop";
		public const string CIBOPNamespaceURI = "www.alphora.com/schemas/ibop";
		public const string CIBOPDiffFound = "diffFound";
		public const string CIBOPOrder = "order";
		public const string CXmlIBOPDiffFound = "{" + CIBOPNamespaceURI + "}" + CIBOPDiffFound;
		public const string CXmlIBOPOrder = "{" + CIBOPNamespaceURI + "}" + CIBOPOrder;

		/// <summary> Adds an attribute to a node. </summary>
		/// <remarks> If the node is null, creates a new node using the ADoc.CreateNode() method. </remarks>
		/// <param name="ADoc"> document to provide context for createting a node </param>
		/// <param name="ANode"> reference to a node </param>
		/// <param name="ANodeName"> name for the node, if it is created </param>
		/// <param name="ANodeNamespaceURI"> namespace uri for the node, if it is careated </param>
		/// <param name="AAttrName"> name of the attribute to add to the node </param>
		/// <param name="AAttrValue"> value for the added attribute </param>
		/// <param name="ANameSpace"> namespace URI for the attribute creation </param>
		protected static void AddAttr
		(
			XDocument ADoc, 
			ref XElement AElement, 
			XName AElementName, 
			XName AAttrName, 
			string AAttrValue
		)
		{
			if (AElement == null)
				AElement = new XElement(AElementName);
			AElement.SetAttributeValue(AAttrName, AAttrValue);
		}

		/// <summary> Copies an element using ADoc to provide the context of the copy. </summary>
		/// <param name="ADoc"> XDocument used to create/host the new node </param>
		/// <param name="ANode"> XElement to copy </param>
		/// <param name="ADeep"> Whether to copy the node's elements as well as attributes </param>
		/// <returns> A copy of ANode in the context of ADoc </returns>
		protected static XElement CopyNode(XDocument ADoc, XElement AElement, bool ADeep)
		{
			XElement LTempElement = null;
			foreach (XAttribute LCopyAttr in AElement.Attributes())
			{
				if (!Persistence.XNamesEqual(LCopyAttr.Name, CXmlIBOPOrder)) // don't copy the ibop:order attribute in the merged doc
				{
					AddAttr
					(
						ADoc, 
						ref LTempElement, 
						AElement.Name, 
						LCopyAttr.Name, 
						LCopyAttr.Value
					);
				}
			}

			if (LTempElement == null)
				LTempElement = new XElement(AElement.Name);

			if (ADeep && AElement.HasElements)
			{
				foreach (XElement LChildNode in AElement.Elements())
					LTempElement.Add(CopyNode(ADoc, LChildNode, true));
			}

			return LTempElement;
		}

		/// <summary> Compares the Modified node's atributes to those of the Original node. </summary>
		/// <remarks> Copies the differences, using AModifiedNode, into ADiffNode. </remarks>
		/// <param name="ADiffDoc"> document used to provide context for the DiffNode </param>
		/// <param name="AModifiedNode"> node that the Original should look like after a merge operation </param>
		/// <param name="AOriginalNode"> node describing the initial state of the node</param>
		/// <param name="ADiffNode"> node describing the operations required to make the Original node into the Modified </param>
		/// <returns> ADiffNode </returns>
		protected static XElement DiffAttrs
		(
			XDocument ADiffDoc, 
			XElement AModifiedElement, 
			XElement AOriginalElement, 
			ref XElement ADiffElement
		)
		{
			// assumes both nodes exist!
			bool LDiffFound = false;

			// assumes the nodes are "matched" in the document by the name attribute, see FindNode()
			foreach (XAttribute LModifiedAttr in AModifiedElement.Attributes())
			{
				XAttribute LOriginalAttr = AOriginalElement.Attribute(LModifiedAttr.Name);

				if (LOriginalAttr == null) // add attr
				{
					LDiffFound = true;
					if (!Persistence.XNamesEqual(LModifiedAttr.Name, CXmlIBOPDiffFound))
						AddAttr(ADiffDoc, ref ADiffElement, AOriginalElement.Name, LModifiedAttr.Name, LModifiedAttr.Value);
				}
				else // compare attr value
				{
					if (!String.Equals(LModifiedAttr.Value, LOriginalAttr.Value))
					{
						LDiffFound = true;
						AddAttr(ADiffDoc, ref ADiffElement, AOriginalElement.Name, LModifiedAttr.Name, LModifiedAttr.Value);
					}
					else
					{
						if 
						(
							String.Equals(LModifiedAttr.Name.NamespaceName, Serializer.CBOPNamespaceURI, StringComparison.OrdinalIgnoreCase) 
								|| String.Equals(LModifiedAttr.Name.NamespaceName, CIBOPNamespaceURI, StringComparison.OrdinalIgnoreCase)
						)
							AddAttr(ADiffDoc, ref ADiffElement, AOriginalElement.Name, LModifiedAttr.Name, LModifiedAttr.Value);
					}
					// delete the original attribute so we know which ones do not appear in the modified node
					AOriginalElement.SetAttributeValue(LOriginalAttr.Name, null);
				}
			}

			foreach (XAttribute LDefaultAttr in AOriginalElement.Attributes())
			{
				// If the attr is in the orig but not in modified, then explicitly mark the attr as "default" (it has changed from something to the default value)
				LDiffFound = true;
				AddAttr
				(
					ADiffDoc,
					ref ADiffElement,
					AOriginalElement.Name,
					XName.Get(Persistence.CBOPDefault + LDefaultAttr.Name, Persistence.CBOPNamespaceURI),
					"True"
				);
			}

			if (LDiffFound)
				AddAttr
				(
					ADiffDoc, 
					ref ADiffElement, 
					AOriginalElement.Name, 
					CXmlIBOPDiffFound, 
					"yes"
				);
			else
				AddAttr
				(
					ADiffDoc, 
					ref ADiffElement, 
					AOriginalElement.Name, 
					CXmlIBOPDiffFound, 
					"no"
				);

			return ADiffElement;
		}

		/// <summary> Finds an element based on the bop:name attribute of AModifiedNode. </summary>
		/// <param name="AModifiedNode"> a selected node in the Modified document </param>
		/// <param name="AOriginalNode"> a "parent-level" node in which to search for a match to AModifiedNode </param>
		/// <param name="APosition"> the position of the node inside the parent </param>
		/// <returns> a OriginalNode's child matching AModifiedNode, or null </returns>
		protected static XElement FindNode(XElement AModifiedElement, XElement AOriginalElement, out int APosition)
		{
			// assumes only searching for resolvable nodes. see IsResolvable()
			
			APosition = -1;

			foreach(XElement LElement in AOriginalElement.Elements())
			{
				APosition++;
				if (AModifiedElement.HasAttributes && LElement.HasAttributes)
				{
					var LModifiedAttr = AModifiedElement.Attribute(Persistence.CXmlBOPName);
					var LOriginalAttr = LElement.Attribute(Persistence.CXmlBOPName);
					if 
					(
						(
							(LModifiedAttr != null) && 
							(LOriginalAttr != null) && 
							String.Equals(LModifiedAttr.Value, LOriginalAttr.Value, StringComparison.OrdinalIgnoreCase)
						)
					)
						return LElement;
				}
			}
			return null;
		}

		protected static XElement FindNode(XElement AElement, string AName)
		{
			if (AElement.HasAttributes) 
			{
				XAttribute LNameAttribute = AElement.Attribute(Persistence.CXmlBOPName);
				if (LNameAttribute != null && LNameAttribute.Value == AName)
					return AElement;
			}
			foreach(XElement LChild in AElement.Elements()) 
			{
				XElement LChildResult = FindNode(LChild, AName);
				if (LChildResult != null)
					return LChildResult;
			}
			return null;
		}

		/// <summary> Test for minimal compliance of attributes against the requirements used in FindNode() </summary>
		/// <param name="ANode"> the node to test for having attributes to use in FindNode() </param>
		/// <returns> True if the node is the root element of the document or it has a name. </returns>
		protected static bool IsResolvable(XElement ANode)
		{
			return Object.ReferenceEquals(ANode, ANode.Document.Root) 
				|| (ANode.Attribute(Persistence.CXmlBOPName) != null);
		}

		/// <summary>
		///		Compares the attributes and children of a node for operations required to
		///		apply to the OriginalNode in a merge operation to make it into the ModifiedNode.
		/// </summary>
		/// <param name="AModifiedNode"> node representing what AOriginalNode should look like after a merge </param>
		/// <param name="AOriginalNode"> node in its "originating" state </param>
		/// <param name="AResultNode"> node containing the operations required to merge AOriginalNode to AModifiedNode.  The "parent" node. </param>
		/// <param name="AResultDoc"> the diff result document </param>
		protected static void DiffNode
		(
			XElement AModifiedNode, 
			XElement AOriginalNode,
			XElement AParentNode, 
			XElement AResultNode,
			XDocument AOriginalDocument
		)
		{
			// if the current node is named
			if (IsResolvable(AModifiedNode))
			{
				if (AResultNode == null)
					AResultNode = new XElement(AModifiedNode.Name);

				// Compare the attributes
				DiffAttrs(AResultNode.Document, AModifiedNode, AOriginalNode, ref AResultNode);

				// Compare the child elements
				int LAdjustment = 0;	// Remembers the position relative to the order of the last node
				int LIndex = 0;			// Absolute index within AModifiedNode's children
				foreach (XElement LModifiedChild in AModifiedNode.Elements())
				{
					if (IsResolvable(LModifiedChild))
					{
						// Search for the child in the immediate children of the matching original node
						int LOriginalChildPosition;
						XElement LOriginalChild = FindNode(LModifiedChild, AOriginalNode, out LOriginalChildPosition);
						bool LFound = LOriginalChild != null;
						
						if (!LFound)
						{
							if (LModifiedChild.HasAttributes)
							{
								XAttribute LNameAttribute = LModifiedChild.Attribute(Persistence.CXmlBOPName);
								if (LNameAttribute != null)
								{
									// if named, search entire original document (moved node case)
									LOriginalChild = FindNode(AOriginalDocument.Root, LNameAttribute.Value);

									// if found, explicitly mark the node as modified
									if (LOriginalChild != null)
										LModifiedChild.SetAttributeValue(CXmlIBOPDiffFound, "yes");
								}
							}

							// if not found in original create a blank node to use as originalnode (new node case)
							if (LOriginalChild == null)
								LOriginalChild = new XElement(LModifiedChild.Name);
						}

						// check placement in original to see if a node needs an order attribute
							// add order attribute if this node isn't beyond the last index of the original (LOriginalNodePosition will be left on the index of the last child if there are none)
						if 
						(
							(LFound && ((LIndex + LAdjustment) != LOriginalChildPosition)) 
								|| (!LFound && ((LIndex + LAdjustment) <= LOriginalChildPosition))
						)
						{
							XElement LTempElement = LModifiedChild;
							// add order attribute
							AddAttr
							(
								AResultNode.Document, 
								ref LTempElement, 
								LModifiedChild.Name, 
								CXmlIBOPOrder, 
								LIndex.ToString()
							);
						}

						if (!LFound)
							LAdjustment--;
			
						// recurse, comparing LOriginalChild to LModifiedChild with AResultNode as the Parent
						DiffNode(LModifiedChild, LOriginalChild, AResultNode, null, AOriginalDocument);
					}

					// Next!
					LIndex++;
				}

				// determine if LNode has changes or children, if so add to AResultNode
				if (AParentNode != null)
				{
					if 
					(
						AResultNode.HasElements || String.Equals(AResultNode.Attribute(CXmlIBOPDiffFound).Value, "yes")
					)
					{
						AResultNode.SetAttributeValue(CXmlIBOPDiffFound, null);
						AParentNode.Add(AResultNode);
					}
				}
				else
				{
					// always insert the document node to make an empty document if no changes found
					AResultNode.SetAttributeValue(CXmlIBOPDiffFound, null);
				}
			}
		}

		/// <summary>
		///		Compares all the nodes, their chldren and attributes to determine what operations
		///		are required to transform the Original document into the Modified document through a
		///		merge operation.
		/// </summary>
		/// <param name="AOriginal"> the "parent" document for inheritance </param>
		/// <param name="AModified"> the "derived" document </param>
		/// <returns> a document containing only the differences between the two documents </returns>
		public static XDocument Diff(XDocument AOriginal, XDocument AModified)
		{
			XDocument LResultDoc = new XDocument();
			LResultDoc.Add(new XElement(AModified.Document.Root.Name));
			LResultDoc.Root.SetAttributeValue(XNamespace.Xmlns + Persistence.CBOPNamespacePrefix, Persistence.CBOPNamespaceURI);
			LResultDoc.Root.SetAttributeValue(XNamespace.Xmlns + CIBOPNamespacePrefix, CIBOPNamespaceURI);
			
			DiffNode(AModified.Root, AOriginal.Root, null, LResultDoc.Root, AOriginal);
			
			// Still needed?
			//LResultDoc.Normalize();

			return LResultDoc;
		}

		public static XDocument Diff(Stream AOriginal, Stream AModified)
		{
			return 
				Diff
				(
					XDocument.Load(XmlReader.Create(AOriginal)), 
					XDocument.Load(XmlReader.Create(AModified))
				);
		}

		/// <summary>
		///		Applies the differences from the diff document to transform the Original document to
		///		the Modified document it was compared against.
		/// </summary>
		/// <param name="ADoc"> the Original document, used to provide contex for created attributes </param>
		/// <param name="AModifiedNode"> the node resulting from a diff operation </param>
		/// <param name="AOriginalNode"> the node to be transformed in the merge </param>
		/// <param name="AIgnoreName"> whether to ignore the name difference in the merge operation </param>
		protected static void MergeAttrs
		(
			XDocument ADoc, 
			XElement AModifiedNode, 
			ref XElement AOriginalNode, 
			bool AIgnoreName
		)
		{
			// assumes the nodes are "matched" in in the document by FindNode()

			if (!AIgnoreName && !Persistence.XNamesEqual(AModifiedNode.Name, AOriginalNode.Name))
			{
				XElement LNewNode = new XElement(AModifiedNode.Name);
				MergeAttrs(ADoc, AOriginalNode, ref LNewNode, true);
				foreach (XElement LNode in AOriginalNode.Elements())
					LNewNode.Add(CopyNode(ADoc, LNode, true));
				AOriginalNode.ReplaceWith(LNewNode);
				AOriginalNode = LNewNode;
				LNewNode = null;
			}
		
			// merge the attributes
			foreach (XAttribute LModifiedAttr in AModifiedNode.Attributes())
			{
				if (!Persistence.XNamesEqual(LModifiedAttr.Name, CXmlIBOPOrder)) // don't put the ibop:order in the merged doc
				{
					// if the attribute name is default-<attribute name> and it's value is true then remove the appropriate attribute from the original node.
					if (LModifiedAttr.Name.NamespaceName == Persistence.CBOPNamespaceURI && LModifiedAttr.Name.LocalName.StartsWith(Persistence.CBOPDefault))
					{
						string LAttributeName = LModifiedAttr.Name.LocalName.Substring(Persistence.CBOPDefault.Length);
						AOriginalNode.SetAttributeValue(LAttributeName, null);
					}
					else
					{
						// assumed each node has a name and never delete the name attr
						XAttribute LOriginalAttr = AOriginalNode.Attribute(LModifiedAttr.Name);
						if (LOriginalAttr == null) // add attr
							AddAttr
							(
								ADoc, 
								ref AOriginalNode, 
								AModifiedNode.Name, 
								LModifiedAttr.Name, 
								LModifiedAttr.Value
							);
						else // change attr value
							LOriginalAttr.Value = LModifiedAttr.Value;
					}
				}
			}
		}

		/// <summary>
		///		Applies the differences in the ModifiedNode to the OriginalNode to transform the
		///		Original document.
		/// </summary>
		/// <param name="AOriginalNode"> node to transform through the merge operation </param>
		/// <param name="AModifiedNode"> node containing the differences to apply </param>
		/// <param name="ADoc"> the Original document used to provide context for created nodes </param>
		protected static void MergeNode
		(
			ref XElement AOriginalNode, 
			XElement AModifiedNode, 
			XDocument ADoc
		)
		{
			// assumes the nodes are "same" based on name and location in the tree
			MergeAttrs(ADoc, AModifiedNode, ref AOriginalNode, false);

			XElement LOriginalNode;
			XElement LPriorNode;

			int LIndex;
			foreach(XElement LChild in AModifiedNode.Elements())
			{
				bool LInsertFlag = false;

				int LOriginalNodePosition;

				LOriginalNode = FindNode(LChild, AOriginalNode, out LOriginalNodePosition);

				if (LOriginalNode != null)
				{
					// check and see if it has an order, if so unhook it and set insertflag so it will be placed in the right spot.
					if (LChild.Attribute(CXmlIBOPOrder) != null) 
					{
						LOriginalNode.Remove();
						LInsertFlag = true;
					}
				} 

				if (LOriginalNode == null)
				{
					// check for moving node
					var LNameAttr = LChild.Attribute(Persistence.CXmlBOPName);
					if (LNameAttr != null)
					{
						// search for node
						LOriginalNode = FindNode(ADoc.Root, LNameAttr.Value);

						// if found unhook it from it's previous parent
						if (LOriginalNode != null) 
							LOriginalNode.Remove();
					}
                    
					// if node wasn't found in original, copy it from diff tree
					if (LOriginalNode == null)
						LOriginalNode = CopyNode(ADoc, LChild, false);

					LInsertFlag = true;
				}

				if (LInsertFlag)
				{
					// read ibop:order if present
					var LOrderAttr = LChild.Attribute(CXmlIBOPOrder);
					if (LOrderAttr != null)
						LIndex = int.Parse(LOrderAttr.Value);
					else
						LIndex = AOriginalNode.Elements().Count();

					// insert the new node
					if ((LIndex > 0) && (LIndex < AOriginalNode.Elements().Count()))
					{
						LPriorNode = AOriginalNode.Elements().ElementAtOrDefault(LIndex - 1);
						if (LPriorNode != null)
							LPriorNode.AddAfterSelf(LOriginalNode);
						else
							AOriginalNode.Add(LOriginalNode);
					}
					else
					{
						if (LIndex >= AOriginalNode.Elements().Count())
							AOriginalNode.Add(LOriginalNode);
						else
							AOriginalNode.AddFirst(LOriginalNode);
					}
				}

				// recurse on the new node
				MergeNode(ref LOriginalNode, LChild, ADoc);
			}
		}

		/// <summary>
		///		Applies differences to the Original document transforming it to a descendant document.
		/// </summary>
		/// <param name="AOriginal"> document to be transformed through the merge operation </param>
		/// <param name="ADiff"> a "diff" document containing the differences to apply </param>
		/// <returns> The Original document transformed by the application of the differences </returns>
		public static XDocument Merge(XDocument AOriginal, XDocument ADiff)
		{

			// ADiff should be the result of a previous Diff operation!
			XElement LOriginalNode = AOriginal.Root;

			MergeNode(ref LOriginalNode, ADiff.Root, AOriginal);

			return AOriginal;
		}
    }
}
