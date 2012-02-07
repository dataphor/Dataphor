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
		public const string IBOPNamespacePrefix = "ibop";
		public const string IBOPNamespaceURI = "www.alphora.com/schemas/ibop";
		public const string IBOPDiffFound = "diffFound";
		public const string IBOPOrder = "order";
		public const string XmlIBOPDiffFound = "{" + IBOPNamespaceURI + "}" + IBOPDiffFound;
		public const string XmlIBOPOrder = "{" + IBOPNamespaceURI + "}" + IBOPOrder;

		/// <summary> Adds an attribute to a node. </summary>
		/// <remarks> If the node is null, creates a new node using the ADoc.CreateNode() method. </remarks>
		/// <param name="doc"> document to provide context for createting a node </param>
		/// <param name="ANode"> reference to a node </param>
		/// <param name="ANodeName"> name for the node, if it is created </param>
		/// <param name="ANodeNamespaceURI"> namespace uri for the node, if it is careated </param>
		/// <param name="attrName"> name of the attribute to add to the node </param>
		/// <param name="attrValue"> value for the added attribute </param>
		/// <param name="ANameSpace"> namespace URI for the attribute creation </param>
		protected static void AddAttr
		(
			XDocument doc, 
			ref XElement element, 
			XName elementName, 
			XName attrName, 
			string attrValue
		)
		{
			if (element == null)
				element = new XElement(elementName);
			element.SetAttributeValue(attrName, attrValue);
		}

		/// <summary> Copies an element using ADoc to provide the context of the copy. </summary>
		/// <param name="doc"> XDocument used to create/host the new node </param>
		/// <param name="ANode"> XElement to copy </param>
		/// <param name="deep"> Whether to copy the node's elements as well as attributes </param>
		/// <returns> A copy of ANode in the context of ADoc </returns>
		protected static XElement CopyNode(XDocument doc, XElement element, bool deep)
		{
			XElement tempElement = null;
			foreach (XAttribute copyAttr in element.Attributes())
			{
				if (!Persistence.XNamesEqual(copyAttr.Name, XmlIBOPOrder)) // don't copy the ibop:order attribute in the merged doc
				{
					AddAttr
					(
						doc, 
						ref tempElement, 
						element.Name, 
						copyAttr.Name, 
						copyAttr.Value
					);
				}
			}

			if (tempElement == null)
				tempElement = new XElement(element.Name);

			if (deep && element.HasElements)
			{
				foreach (XElement childNode in element.Elements())
					tempElement.Add(CopyNode(doc, childNode, true));
			}

			return tempElement;
		}

		/// <summary> Compares the Modified node's atributes to those of the Original node. </summary>
		/// <remarks> Copies the differences, using AModifiedNode, into ADiffNode. </remarks>
		/// <param name="diffDoc"> document used to provide context for the DiffNode </param>
		/// <param name="AModifiedNode"> node that the Original should look like after a merge operation </param>
		/// <param name="AOriginalNode"> node describing the initial state of the node</param>
		/// <param name="ADiffNode"> node describing the operations required to make the Original node into the Modified </param>
		/// <returns> ADiffNode </returns>
		protected static XElement DiffAttrs
		(
			XDocument diffDoc, 
			XElement modifiedElement, 
			XElement originalElement, 
			ref XElement diffElement
		)
		{
			// assumes both nodes exist!
			bool diffFound = false;

			// assumes the nodes are "matched" in the document by the name attribute, see FindNode()
			foreach (XAttribute modifiedAttr in modifiedElement.Attributes())
			{
				XAttribute originalAttr = originalElement.Attribute(modifiedAttr.Name);

				if (originalAttr == null) // add attr
				{
					diffFound = true;
					if (!Persistence.XNamesEqual(modifiedAttr.Name, XmlIBOPDiffFound))
						AddAttr(diffDoc, ref diffElement, originalElement.Name, modifiedAttr.Name, modifiedAttr.Value);
				}
				else // compare attr value
				{
					if (!String.Equals(modifiedAttr.Value, originalAttr.Value))
					{
						diffFound = true;
						AddAttr(diffDoc, ref diffElement, originalElement.Name, modifiedAttr.Name, modifiedAttr.Value);
					}
					else
					{
						if 
						(
							String.Equals(modifiedAttr.Name.NamespaceName, Serializer.BOPNamespaceURI, StringComparison.OrdinalIgnoreCase) 
								|| String.Equals(modifiedAttr.Name.NamespaceName, IBOPNamespaceURI, StringComparison.OrdinalIgnoreCase)
						)
							AddAttr(diffDoc, ref diffElement, originalElement.Name, modifiedAttr.Name, modifiedAttr.Value);
					}
					// delete the original attribute so we know which ones do not appear in the modified node
					originalElement.SetAttributeValue(originalAttr.Name, null);
				}
			}

			foreach (XAttribute defaultAttr in originalElement.Attributes())
			{
				// If the attr is in the orig but not in modified, then explicitly mark the attr as "default" (it has changed from something to the default value)
				diffFound = true;
				AddAttr
				(
					diffDoc,
					ref diffElement,
					originalElement.Name,
					XName.Get(Persistence.BOPDefault + defaultAttr.Name, Persistence.BOPNamespaceURI),
					"True"
				);
			}

			if (diffFound)
				AddAttr
				(
					diffDoc, 
					ref diffElement, 
					originalElement.Name, 
					XmlIBOPDiffFound, 
					"yes"
				);
			else
				AddAttr
				(
					diffDoc, 
					ref diffElement, 
					originalElement.Name, 
					XmlIBOPDiffFound, 
					"no"
				);

			return diffElement;
		}

		/// <summary> Finds an element based on the bop:name attribute of AModifiedNode. </summary>
		/// <param name="AModifiedNode"> a selected node in the Modified document </param>
		/// <param name="AOriginalNode"> a "parent-level" node in which to search for a match to AModifiedNode </param>
		/// <param name="position"> the position of the node inside the parent </param>
		/// <returns> a OriginalNode's child matching AModifiedNode, or null </returns>
		protected static XElement FindNode(XElement modifiedElement, XElement originalElement, out int position)
		{
			// assumes only searching for resolvable nodes. see IsResolvable()
			
			position = -1;

			foreach(XElement element in originalElement.Elements())
			{
				position++;
				if (modifiedElement.HasAttributes && element.HasAttributes)
				{
					var modifiedAttr = modifiedElement.Attribute(Persistence.XmlBOPName);
					var originalAttr = element.Attribute(Persistence.XmlBOPName);
					if 
					(
						(
							(modifiedAttr != null) && 
							(originalAttr != null) && 
							String.Equals(modifiedAttr.Value, originalAttr.Value, StringComparison.OrdinalIgnoreCase)
						)
					)
						return element;
				}
			}
			return null;
		}

		protected static XElement FindNode(XElement element, string name)
		{
			if (element.HasAttributes) 
			{
				XAttribute nameAttribute = element.Attribute(Persistence.XmlBOPName);
				if (nameAttribute != null && nameAttribute.Value == name)
					return element;
			}
			foreach(XElement child in element.Elements()) 
			{
				XElement childResult = FindNode(child, name);
				if (childResult != null)
					return childResult;
			}
			return null;
		}

		/// <summary> Test for minimal compliance of attributes against the requirements used in FindNode() </summary>
		/// <param name="node"> the node to test for having attributes to use in FindNode() </param>
		/// <returns> True if the node is the root element of the document or it has a name. </returns>
		protected static bool IsResolvable(XElement node)
		{
			return Object.ReferenceEquals(node, node.Document.Root) 
				|| (node.Attribute(Persistence.XmlBOPName) != null);
		}

		/// <summary>
		///		Compares the attributes and children of a node for operations required to
		///		apply to the OriginalNode in a merge operation to make it into the ModifiedNode.
		/// </summary>
		/// <param name="modifiedNode"> node representing what AOriginalNode should look like after a merge </param>
		/// <param name="originalNode"> node in its "originating" state </param>
		/// <param name="resultNode"> node containing the operations required to merge AOriginalNode to AModifiedNode.  The "parent" node. </param>
		/// <param name="AResultDoc"> the diff result document </param>
		protected static void DiffNode
		(
			XElement modifiedNode, 
			XElement originalNode,
			XElement parentNode, 
			XElement resultNode,
			XDocument originalDocument
		)
		{
			// if the current node is named
			if (IsResolvable(modifiedNode))
			{
				if (resultNode == null)
					resultNode = new XElement(modifiedNode.Name);

				// Compare the attributes
				DiffAttrs(resultNode.Document, modifiedNode, originalNode, ref resultNode);

				// Compare the child elements
				int adjustment = 0;	// Remembers the position relative to the order of the last node
				int index = 0;			// Absolute index within AModifiedNode's children
				foreach (XElement modifiedChild in modifiedNode.Elements())
				{
					if (IsResolvable(modifiedChild))
					{
						// Search for the child in the immediate children of the matching original node
						int originalChildPosition;
						XElement originalChild = FindNode(modifiedChild, originalNode, out originalChildPosition);
						bool found = originalChild != null;
						
						if (!found)
						{
							if (modifiedChild.HasAttributes)
							{
								XAttribute nameAttribute = modifiedChild.Attribute(Persistence.XmlBOPName);
								if (nameAttribute != null)
								{
									// if named, search entire original document (moved node case)
									originalChild = FindNode(originalDocument.Root, nameAttribute.Value);

									// if found, explicitly mark the node as modified
									if (originalChild != null)
										modifiedChild.SetAttributeValue(XmlIBOPDiffFound, "yes");
								}
							}

							// if not found in original create a blank node to use as originalnode (new node case)
							if (originalChild == null)
								originalChild = new XElement(modifiedChild.Name);
						}

						// check placement in original to see if a node needs an order attribute
							// add order attribute if this node isn't beyond the last index of the original (LOriginalNodePosition will be left on the index of the last child if there are none)
						if 
						(
							(found && ((index + adjustment) != originalChildPosition)) 
								|| (!found && ((index + adjustment) <= originalChildPosition))
						)
						{
							XElement tempElement = modifiedChild;
							// add order attribute
							AddAttr
							(
								resultNode.Document, 
								ref tempElement, 
								modifiedChild.Name, 
								XmlIBOPOrder, 
								index.ToString()
							);
						}

						if (!found)
							adjustment--;
			
						// recurse, comparing LOriginalChild to LModifiedChild with AResultNode as the Parent
						DiffNode(modifiedChild, originalChild, resultNode, null, originalDocument);
					}

					// Next!
					index++;
				}

				// determine if LNode has changes or children, if so add to AResultNode
				if (parentNode != null)
				{
					if 
					(
						resultNode.HasElements || String.Equals(resultNode.Attribute(XmlIBOPDiffFound).Value, "yes")
					)
					{
						resultNode.SetAttributeValue(XmlIBOPDiffFound, null);
						parentNode.Add(resultNode);
					}
				}
				else
				{
					// always insert the document node to make an empty document if no changes found
					resultNode.SetAttributeValue(XmlIBOPDiffFound, null);
				}
			}
		}

		/// <summary>
		///		Compares all the nodes, their chldren and attributes to determine what operations
		///		are required to transform the Original document into the Modified document through a
		///		merge operation.
		/// </summary>
		/// <param name="original"> the "parent" document for inheritance </param>
		/// <param name="modified"> the "derived" document </param>
		/// <returns> a document containing only the differences between the two documents </returns>
		public static XDocument Diff(XDocument original, XDocument modified)
		{
			XDocument resultDoc = new XDocument();
			resultDoc.Add(new XElement(modified.Document.Root.Name));
			resultDoc.Root.SetAttributeValue(XNamespace.Xmlns + Persistence.BOPNamespacePrefix, Persistence.BOPNamespaceURI);
			resultDoc.Root.SetAttributeValue(XNamespace.Xmlns + IBOPNamespacePrefix, IBOPNamespaceURI);
			
			DiffNode(modified.Root, original.Root, null, resultDoc.Root, original);
			
			// Still needed?
			//LResultDoc.Normalize();

			return resultDoc;
		}

		public static XDocument Diff(Stream original, Stream modified)
		{
			return 
				Diff
				(
					XDocument.Load(XmlReader.Create(original)), 
					XDocument.Load(XmlReader.Create(modified))
				);
		}

		/// <summary>
		///		Applies the differences from the diff document to transform the Original document to
		///		the Modified document it was compared against.
		/// </summary>
		/// <param name="doc"> the Original document, used to provide contex for created attributes </param>
		/// <param name="modifiedNode"> the node resulting from a diff operation </param>
		/// <param name="originalNode"> the node to be transformed in the merge </param>
		/// <param name="ignoreName"> whether to ignore the name difference in the merge operation </param>
		protected static void MergeAttrs
		(
			XDocument doc, 
			XElement modifiedNode, 
			ref XElement originalNode, 
			bool ignoreName
		)
		{
			// assumes the nodes are "matched" in in the document by FindNode()

			if (!ignoreName && !Persistence.XNamesEqual(modifiedNode.Name, originalNode.Name))
			{
				XElement newNode = new XElement(modifiedNode.Name);
				MergeAttrs(doc, originalNode, ref newNode, true);
				foreach (XElement node in originalNode.Elements())
					newNode.Add(CopyNode(doc, node, true));
				originalNode.ReplaceWith(newNode);
				originalNode = newNode;
				newNode = null;
			}
		
			// merge the attributes
			foreach (XAttribute modifiedAttr in modifiedNode.Attributes())
			{
				if (!(modifiedAttr.Name == (XNamespace.Xmlns + IBOPNamespacePrefix)) && !Persistence.XNamesEqual(modifiedAttr.Name, XmlIBOPOrder)) // don't put the ibop:order in the merged doc
				{
					// if the attribute name is default-<attribute name> and it's value is true then remove the appropriate attribute from the original node.
					if (modifiedAttr.Name.NamespaceName == Persistence.BOPNamespaceURI && modifiedAttr.Name.LocalName.StartsWith(Persistence.BOPDefault))
					{
						string attributeName = modifiedAttr.Name.LocalName.Substring(Persistence.BOPDefault.Length);
						originalNode.SetAttributeValue(attributeName, null);
					}
					else
					{
						// assumed each node has a name and never delete the name attr
						XAttribute originalAttr = originalNode.Attribute(modifiedAttr.Name);
						if (originalAttr == null) // add attr
							AddAttr
							(
								doc, 
								ref originalNode, 
								modifiedNode.Name, 
								modifiedAttr.Name, 
								modifiedAttr.Value
							);
						else // change attr value
							originalAttr.Value = modifiedAttr.Value;
					}
				}
			}
		}

		/// <summary>
		///		Applies the differences in the ModifiedNode to the OriginalNode to transform the
		///		Original document.
		/// </summary>
		/// <param name="originalNode"> node to transform through the merge operation </param>
		/// <param name="modifiedNode"> node containing the differences to apply </param>
		/// <param name="doc"> the Original document used to provide context for created nodes </param>
		protected static void MergeNode
		(
			ref XElement originalNode, 
			XElement modifiedNode, 
			XDocument doc
		)
		{
			// assumes the nodes are "same" based on name and location in the tree
			MergeAttrs(doc, modifiedNode, ref originalNode, false);

			XElement localOriginalNode;
			XElement priorNode;

			int index;
			foreach(XElement child in modifiedNode.Elements())
			{
				bool insertFlag = false;

				int originalNodePosition;

				localOriginalNode = FindNode(child, originalNode, out originalNodePosition);

				if (localOriginalNode != null)
				{
					// check and see if it has an order, if so unhook it and set insertflag so it will be placed in the right spot.
					if (child.Attribute(XmlIBOPOrder) != null) 
					{
						localOriginalNode.Remove();
						insertFlag = true;
					}
				} 

				if (localOriginalNode == null)
				{
					// check for moving node
					var nameAttr = child.Attribute(Persistence.XmlBOPName);
					if (nameAttr != null)
					{
						// search for node
						localOriginalNode = FindNode(doc.Root, nameAttr.Value);

						// if found unhook it from its previous parent
						if (localOriginalNode != null) 
							localOriginalNode.Remove();
					}
                    
					// if node wasn't found in original, copy it from diff tree
					if (localOriginalNode == null)
						localOriginalNode = CopyNode(doc, child, false);

					insertFlag = true;
				}

				if (insertFlag)
				{
					// read ibop:order if present
					var orderAttr = child.Attribute(XmlIBOPOrder);
					if (orderAttr != null)
						index = int.Parse(orderAttr.Value);
					else
						index = originalNode.Elements().Count();

					// insert the new node
					if ((index > 0) && (index < originalNode.Elements().Count()))
					{
						priorNode = originalNode.Elements().ElementAtOrDefault(index - 1);
						if (priorNode != null)
							priorNode.AddAfterSelf(localOriginalNode);
						else
							originalNode.Add(localOriginalNode);
					}
					else
					{
						if (index >= originalNode.Elements().Count())
							originalNode.Add(localOriginalNode);
						else
							originalNode.AddFirst(localOriginalNode);
					}
				}

				// recurse on the new node
				MergeNode(ref localOriginalNode, child, doc);
			}
		}

		/// <summary>
		///		Applies differences to the Original document transforming it to a descendant document.
		/// </summary>
		/// <param name="original"> document to be transformed through the merge operation </param>
		/// <param name="diff"> a "diff" document containing the differences to apply </param>
		/// <returns> The Original document transformed by the application of the differences </returns>
		public static XDocument Merge(XDocument original, XDocument diff)
		{

			// ADiff should be the result of a previous Diff operation!
			XElement originalNode = original.Root;

			MergeNode(ref originalNode, diff.Root, original);

			return original;
		}
    }
}
