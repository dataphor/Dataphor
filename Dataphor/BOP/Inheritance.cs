/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Xml;

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
		public const string CIBOPNamespaceURI = "www.alphora.com/schemas/ibop";
		public const string CIBOPDiffFound = "diffFound";
		public const string CIBOPOrder = "order";
		public const string CXmlIBOPDiffFound = "ibop:" + CIBOPDiffFound;
		public const string CXmlIBOPOrder = "ibop:" + CIBOPOrder;

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
			XmlDocument ADoc, 
			ref XmlNode ANode, 
			string ANodeName, 
			string ANodeNamespaceURI, 
			string AAttrName, 
			string AAttrValue, 
			string ANameSpace
		)
		{
			XmlAttribute LAttr;
			if (ANode == null)
				ANode = ADoc.CreateNode(XmlNodeType.Element, ANodeName, ANodeNamespaceURI);
			LAttr = ADoc.CreateAttribute(AAttrName, ANameSpace);
			LAttr.Value = AAttrValue;
			ANode.Attributes.SetNamedItem(LAttr);
		}

		/// <summary> Copies a node using ADoc to provide the context of the copy. </summary>
		/// <param name="ADoc"> XmlDocument used to create/host the new node </param>
		/// <param name="ANode"> XmlNode to copy </param>
		/// <param name="ADeep"> whether to copy the nodes children as well as attributes </param>
		/// <returns> a copy of ANode in the context of ADoc </returns>
		protected static XmlNode CopyNode(XmlDocument ADoc, XmlNode ANode, bool ADeep)
		{
			/* this method is required to work around the document context-sensitive problem
			 * with the XmlNode.Clone() and .CloneNode() methods.
			 */
			XmlNode LtmpNode = null;
			if (ANode.Attributes != null)
				foreach (XmlAttribute LCopyAttr in ANode.Attributes)
				{
					if (LCopyAttr.Name != CXmlIBOPOrder) // don't put the ibop:order attribute in the merged doc
					{
						AddAttr
						(
							ADoc, 
							ref LtmpNode, 
							ANode.Name, 
							ANode.NamespaceURI, 
							LCopyAttr.Name, 
							LCopyAttr.Value, 
							LCopyAttr.NamespaceURI
						);
					}
				}

			if (LtmpNode == null)
				if (ANode.NamespaceURI == "" &&
					(ANode.Name.StartsWith("bop:", StringComparison.OrdinalIgnoreCase) || ANode.Name.StartsWith("ibop:", StringComparison.OrdinalIgnoreCase)))
				{
					if (ANode.Name.StartsWith("bop:", StringComparison.OrdinalIgnoreCase))
						LtmpNode = ADoc.CreateNode(ANode.NodeType, ANode.Name, Serializer.CBOPNamespaceURI);
					else
						if (ANode.Name.StartsWith("ibop:", StringComparison.OrdinalIgnoreCase))
							LtmpNode = ADoc.CreateNode(ANode.NodeType, ANode.Name, CIBOPNamespaceURI);
				}
				else
					LtmpNode = ADoc.CreateNode(ANode.NodeType, ANode.Name, ANode.NamespaceURI);

			if (ADeep && ANode.HasChildNodes == true)
			{
				foreach(XmlNode LChildNode in ANode.ChildNodes)
				{
					LtmpNode.AppendChild(CopyNode(ADoc, LChildNode, true));
				}
			}
			if (LtmpNode.NodeType != XmlNodeType.Element)
				LtmpNode.Value = ANode.Value;

			return LtmpNode;
		}

		/// <summary> Compares the Modified node's atributes to those of the Original node. </summary>
		/// <remarks> Copies the differences, using AModifiedNode, into ADiffNode. </remarks>
		/// <param name="ADiffDoc"> document used to provide context for the DiffNode </param>
		/// <param name="AModifiedNode"> node that the Original should look like after a merge operation </param>
		/// <param name="AOriginalNode"> node describing the initial state of the node</param>
		/// <param name="ADiffNode"> node describing the operations required to make the Original node into the Modified </param>
		/// <returns> ADiffNode </returns>
		protected static XmlNode DiffAttrs
		(
			XmlDocument ADiffDoc, 
			XmlNode AModifiedNode, 
			XmlNode AOriginalNode, 
			ref XmlNode ADiffNode
		)
		{
			// assumes both nodes exist!
			XmlAttribute LOriginalAttr;
			XmlAttribute LModifiedAttr;
			bool LDiffFound = false;

			// assumes the nodes are "matched" in the document by the name attribute, see FindNode()
			for (int i = AModifiedNode.Attributes.Count - 1; i >= 0; i--)
			{
				LModifiedAttr = AModifiedNode.Attributes[i];
				LOriginalAttr = AOriginalNode.Attributes[LModifiedAttr.Name];

				if (LOriginalAttr == null) // add attr
				{
					LDiffFound = true;
					if (!LModifiedAttr.Name.Equals(CXmlIBOPDiffFound, StringComparison.OrdinalIgnoreCase))
						AddAttr
						(
							ADiffDoc, 
							ref ADiffNode, 
							AOriginalNode.Name, 
							AOriginalNode.NamespaceURI, 
							LModifiedAttr.Name, 
							LModifiedAttr.Value, 
							LModifiedAttr.NamespaceURI
						);
				}
				else // compare attr value
				{
					if (!LModifiedAttr.Value.Equals(LOriginalAttr.Value))
					{
						LDiffFound = true;
						AddAttr
						(
							ADiffDoc, 
							ref ADiffNode, 
							AOriginalNode.Name, 
							AOriginalNode.NamespaceURI, 
							LModifiedAttr.Name, 
							LModifiedAttr.Value, 
							LModifiedAttr.NamespaceURI
						);
				}
					else
					{
						if (LModifiedAttr.Name.StartsWith("bop:", StringComparison.OrdinalIgnoreCase) ||
							LModifiedAttr.Name.StartsWith("ibop:", StringComparison.OrdinalIgnoreCase))
							AddAttr
							(
								ADiffDoc, 
								ref ADiffNode, 
								AOriginalNode.Name, 
								AOriginalNode.NamespaceURI, 
								LModifiedAttr.Name, 
								LModifiedAttr.Value, 
								LModifiedAttr.NamespaceURI
							);
					}
					// delete the original attribute so we know which ones do not appear in the modified node
					AOriginalNode.Attributes.Remove(LOriginalAttr);
				}
			}

			foreach (XmlAttribute LDefaultAttr in AOriginalNode.Attributes)
			{
				// If the attr is in the orig but not in modified, then explicitly mark the attr as "default" (it has changed from something to the default value)
				LDiffFound = true;
				AddAttr
				(
					ADiffDoc,
					ref ADiffNode,
					AOriginalNode.Name,
                    AOriginalNode.NamespaceURI,
					Persistence.CXmlBOPDefault + LDefaultAttr.Name,
					"True",
					Persistence.CBOPNamespaceURI
				);
			}

			if (LDiffFound)
				AddAttr
				(
					ADiffDoc, 
					ref ADiffNode, 
					AOriginalNode.Name, 
					AOriginalNode.NamespaceURI, 
					CXmlIBOPDiffFound, 
					"yes",
					CIBOPNamespaceURI
				);
			else
				AddAttr
				(
					ADiffDoc, 
					ref ADiffNode, 
					AOriginalNode.Name, 
					AOriginalNode.NamespaceURI, 
					CXmlIBOPDiffFound, 
					"no",
					CIBOPNamespaceURI
				);

			return ADiffNode;
		}

		/// <summary> Finds a node based on the bop:name attribute of AModifiedNode. </summary>
		/// <param name="AModifiedNode"> a selected node in the Modified document </param>
		/// <param name="AOriginalNode"> a "parent-level" node in which to search for a match to AModifiedNode </param>
		/// <param name="APosition"> the position of the node inside the parent </param>
		/// <returns> a OriginalNode's child matching AModifiedNode, or null </returns>
		protected static XmlNode FindNode(XmlNode AModifiedNode, XmlNode AOriginalNode, out int APosition)
		{
			// assumes only searching for resolvable nodes. see IsResolvable()
			
			XmlNode LModifiedAttr;
			XmlNode LOriginalAttr;

			APosition = -1;

			foreach(XmlNode LNode in AOriginalNode.ChildNodes)
			{
				if (LNode is XmlElement)
				{
					APosition++;
					if ((AModifiedNode.Attributes != null) && (LNode.Attributes != null))
					{
						LModifiedAttr = AModifiedNode.Attributes.GetNamedItem(Persistence.CXmlBOPName);
						LOriginalAttr = LNode.Attributes.GetNamedItem(Persistence.CXmlBOPName);
						if 
						(
							(
								(LModifiedAttr != null) && 
								(LOriginalAttr != null) && 
								(String.Compare(LModifiedAttr.Value, LOriginalAttr.Value, true) == 0)
							)
						)
							return LNode;
					}
				}
			}
			return null;
		}

		protected static XmlNode FindNode(XmlNode AXmlNode, string AName)
		{
			if (AXmlNode.Attributes != null) 
			{
				XmlNode LNameAttribute = AXmlNode.Attributes.GetNamedItem(Persistence.CXmlBOPName);
				if (LNameAttribute != null && LNameAttribute.Value == AName)
					return AXmlNode;
			}
			foreach(XmlNode LChild in AXmlNode.ChildNodes) 
			{
				XmlNode LTemp = FindNode(LChild, AName);
				if (LTemp != null)
					return LTemp;
			}
			return null;
		}

		/// <summary> Test for minimal compliance of attributes against the requirements used in FindNode() </summary>
		/// <param name="ANode"> the node to test for having attributes to use in FindNode() </param>
		/// <returns> true if the node might be located in the Original document </returns>
		protected static bool IsResolvable(XmlNode ANode)
		{
			/* tests for minimal compliance
			 * all nodes must have a bop:name in order to persist in inheritance
			 */
			if (Object.ReferenceEquals(ANode, ANode.OwnerDocument.DocumentElement))
				return true;
		
			if (ANode.Attributes == null)
				return false;
			
			return (ANode.Attributes.GetNamedItem(Persistence.CXmlBOPName) != null);
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
			XmlNode AModifiedNode, 
			XmlNode AOriginalNode,
			XmlNode AParentNode, 
			XmlNode AResultNode
		)
		{
			// if the current node is named
			if (IsResolvable(AModifiedNode))
			{
				if (AResultNode == null)
					AResultNode = AParentNode.OwnerDocument.CreateElement(AModifiedNode.Name, AModifiedNode.NamespaceURI);

				// Compare the attributes
				DiffAttrs(AResultNode.OwnerDocument, AModifiedNode, AOriginalNode, ref AResultNode);

				// Compare the child elements
				XmlNode LModifiedChild = AModifiedNode.FirstChild;
				XmlNode LOriginalChild;
				int LAdjustment = 0;	// Remembers the position relative to the order of the last node
				int LIndex = 0;			// Absolute index within AModifiedNode's children
				while (LModifiedChild != null)
				{
					if (IsResolvable(LModifiedChild))
					{
						// Search for the child in the immediate children of the matching original node
						int LOriginalChildPosition;
						LOriginalChild = FindNode(LModifiedChild, AOriginalNode, out LOriginalChildPosition);
						bool LFound = LOriginalChild != null;
						
						if (!LFound)
						{
							if (LModifiedChild.Attributes != null)
							{
								XmlAttribute LNameAttribute = LModifiedChild.Attributes[Persistence.CXmlBOPName];
								if (LNameAttribute != null)
								{
									// if named, search entire original document (moved node case)
									LOriginalChild = FindNode(AOriginalNode.OwnerDocument, LNameAttribute.Value);

									// if found, explicitly mark the node as modified
									if (LOriginalChild != null)
									{
										XmlAttribute LAttribute = LModifiedChild.OwnerDocument.CreateAttribute(CXmlIBOPDiffFound, CIBOPNamespaceURI);
										LAttribute.Value = "yes";
										LModifiedChild.Attributes.Append(LAttribute);
									}
								}
							}

							// if not found in original create a blank node to use as originalnode (new node case)
							if (LOriginalChild == null)
								LOriginalChild = AOriginalNode.OwnerDocument.CreateNode(XmlNodeType.Element, LModifiedChild.Name, LModifiedChild.NamespaceURI);
						}

						// check placement in original to see if a node needs an order attribute
							// add order attribute if this node isn't beyond the last index of the original (LOriginalNodePosition will be left on the index of the last child if there are none)
						if 
						(
							(LFound && ((LIndex + LAdjustment) != LOriginalChildPosition)) 
								|| (!LFound && ((LIndex + LAdjustment) <= LOriginalChildPosition))
						)
						{
							// add order attribute
							AddAttr
							(
								AResultNode.OwnerDocument, 
								ref LModifiedChild, 
								LModifiedChild.Name, 
								LModifiedChild.NamespaceURI, 
								CXmlIBOPOrder, 
								LIndex.ToString(),
								CIBOPNamespaceURI
							);
						}

						if (!LFound)
							LAdjustment--;
			
						// recurse, comparing LOriginalChild to LModifiedChild with AResultNode as the Parent
						DiffNode(LModifiedChild, LOriginalChild, AResultNode, null);
					}

					// Next!
					LModifiedChild = LModifiedChild.NextSibling;
					LIndex++;
				}

				// determine if LNode has changes or children, if so add to AResultNode
				if (AParentNode != null)
				{
					if 
					(
						AResultNode.ChildNodes.Count > 0 || 
						AResultNode.Attributes[CXmlIBOPDiffFound].Value.Equals("yes")
					)
					{
						AResultNode.Attributes.Remove
						(
							(XmlAttribute)AResultNode.Attributes[CXmlIBOPDiffFound]
						);
						AParentNode.AppendChild(AResultNode);
					}
				}
				else
				{
					// always insert the document node to make an empty document if no changes found
					AResultNode.Attributes.Remove((XmlAttribute)AResultNode.Attributes[CXmlIBOPDiffFound]);
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
		public static XmlDocument Diff(XmlDocument AOriginal, XmlDocument AModified)
		{
			XmlDocument LResultDoc = new XmlDocument();
			LResultDoc.AppendChild(LResultDoc.CreateElement(AModified.DocumentElement.Name));

			XmlAttribute LAttribute = LResultDoc.CreateAttribute("xmlns:ibop");
			LAttribute.Value = CIBOPNamespaceURI;
			LResultDoc.DocumentElement.Attributes.Prepend(LAttribute);

			LAttribute = LResultDoc.CreateAttribute("xmlns:bop");
			LAttribute.Value = Serializer.CBOPNamespaceURI;
			LResultDoc.DocumentElement.Attributes.Prepend(LAttribute);
			
			DiffNode(AModified.DocumentElement, AOriginal.DocumentElement, null, LResultDoc.DocumentElement);
			
			LResultDoc.Normalize();

			return LResultDoc;
		}

		public static XmlDocument Diff(Stream AOriginal, Stream AModified)
		{
			XmlTextReader LReader = new XmlTextReader(AOriginal);
			XmlDocument LOriginalDoc = new XmlDocument();
			LOriginalDoc.Load(LReader);

			LReader = new XmlTextReader(AModified);
			XmlDocument LModifiedDoc = new XmlDocument();
			LModifiedDoc.Load(LReader);

			return Diff(LOriginalDoc, LModifiedDoc);
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
			XmlDocument ADoc, 
			XmlNode AModifiedNode, 
			ref XmlNode AOriginalNode, 
			bool AIgnoreName
		)
		{
			XmlAttribute LOriginalAttr;
			XmlNode LTmpNode;
			// assumes the nodes are "matched" in in the document by FindNode()

			if (!AIgnoreName && !AModifiedNode.Name.Equals(AOriginalNode.Name, StringComparison.OrdinalIgnoreCase))
			{
				LTmpNode = ADoc.CreateNode
					(
						AModifiedNode.NodeType, 
						AModifiedNode.Name, 
						AModifiedNode.NamespaceURI
					);
				MergeAttrs(ADoc, AOriginalNode, ref LTmpNode, true);
				foreach (XmlNode LNode in AOriginalNode)
				{
					LTmpNode.AppendChild(CopyNode(ADoc, LNode, true));
				}
				AOriginalNode.ParentNode.ReplaceChild(LTmpNode, AOriginalNode);
				AOriginalNode = LTmpNode;
				LTmpNode = null;
			}
		
			// merge the attributes
			foreach (XmlAttribute LModifiedAttr in AModifiedNode.Attributes)
			{
				if (!LModifiedAttr.Name.Equals("xmlns:ibop", StringComparison.OrdinalIgnoreCase) && !LModifiedAttr.Name.Equals(CXmlIBOPOrder, StringComparison.OrdinalIgnoreCase)) // don't put the xmlns:ibop in the merged doc
				{
					// if the attribute name is default-<attribute name> and it's value is true then remove the appropriate attribute from the original node.
					if (LModifiedAttr.Name.StartsWith(Persistence.CXmlBOPDefault, StringComparison.OrdinalIgnoreCase))
					{
						string LAttributeName = LModifiedAttr.Name.Substring(Persistence.CXmlBOPDefault.Length, LModifiedAttr.Name.Length - Persistence.CXmlBOPDefault.Length);
						XmlAttribute LAttribute = AOriginalNode.Attributes[LAttributeName];
						if (LAttribute != null)
							AOriginalNode.Attributes.Remove(LAttribute);
					}
					else
					{
						// assumed each node has a name and never delete the name attr
						LOriginalAttr = AOriginalNode.Attributes[LModifiedAttr.Name];
						if (LOriginalAttr == null) // add attr
							AddAttr
							(
								ADoc, 
								ref AOriginalNode, 
								AModifiedNode.Name, 
								AModifiedNode.NamespaceURI, 
								LModifiedAttr.Name, 
								LModifiedAttr.Value, 
								LModifiedAttr.NamespaceURI
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
			ref XmlNode AOriginalNode, 
			XmlNode AModifiedNode, 
			XmlDocument ADoc
		)
		{
			// assumes the nodes are "same" based on name and location in the tree
			MergeAttrs(ADoc, AModifiedNode, ref AOriginalNode, false);

			XmlNode LOriginalNode;
			XmlNode LPriorNode;

			int LIndex;
			foreach(XmlNode LChild in AModifiedNode.ChildNodes)
			{
				bool LInsertFlag = false;

				int LOriginalNodePosition;

				LOriginalNode = FindNode(LChild, AOriginalNode, out LOriginalNodePosition);

				if (LOriginalNode != null)
				{
					// check and see if it has an order, if so unhook it and set insertflag so it will be placed in the right spot.
					if ((LChild.Attributes != null) && LChild.Attributes.GetNamedItem(CXmlIBOPOrder) != null) 
					{
						LOriginalNode.ParentNode.RemoveChild(LOriginalNode);
						LInsertFlag = true;
					}
				} 

				if (LOriginalNode == null)
				{
					// check for moving node
					if ((LChild.Attributes != null) && LChild.Attributes.GetNamedItem(Persistence.CXmlBOPName) != null)
					{
						// search for node
						LOriginalNode = FindNode(ADoc, LChild.Attributes.GetNamedItem(Persistence.CXmlBOPName).Value);

						// if found unhook it from it's previous parent
						if (LOriginalNode != null) 
							LOriginalNode.ParentNode.RemoveChild(LOriginalNode);
					}
                    
					// if node wasn't found in original, copy it from diff tree
					if (LOriginalNode == null)
						LOriginalNode = CopyNode(ADoc, LChild, false);

					LInsertFlag = true;
				}

				if (LInsertFlag)
				{
					// read ibop:order if present
					if ((LChild.Attributes != null) && (LChild.Attributes[CXmlIBOPOrder] != null))
						LIndex = int.Parse(LChild.Attributes[CXmlIBOPOrder].Value);
					else
						LIndex = AOriginalNode.ChildNodes.Count;

					// insert the new node
					if ((LIndex > 0) && (LIndex < AOriginalNode.ChildNodes.Count))
					{
						LPriorNode = AOriginalNode.ChildNodes[LIndex - 1];
						if (LPriorNode != null)
							AOriginalNode.InsertAfter(LOriginalNode, LPriorNode);
						else
							AOriginalNode.AppendChild(LOriginalNode);
					}
					else
					{
						if (LIndex >= AOriginalNode.ChildNodes.Count)
							AOriginalNode.AppendChild(LOriginalNode);
						else
							AOriginalNode.PrependChild(LOriginalNode);
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
		public static XmlDocument Merge(XmlDocument AOriginal, XmlDocument ADiff)
		{

			// ADiff should be the result of a previous Diff operation!
			XmlNode LOriginalNode = AOriginal.DocumentElement;

			MergeNode(ref LOriginalNode, ADiff.DocumentElement, AOriginal);

			return AOriginal;
		}
    }
}
