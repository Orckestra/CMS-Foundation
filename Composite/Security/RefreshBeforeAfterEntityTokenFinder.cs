using System;
using System.Linq;
using System.Collections.Generic;
using Composite.Logging;
using Composite.Types;


namespace Composite.Security
{
    public static class RefreshBeforeAfterEntityTokenFinder
    {
        public static IEnumerable<EntityToken> FindEntityTokens(RelationshipGraph beforeGraph, RelationshipGraph afterGraph)
        {
            if (beforeGraph == null) throw new ArgumentNullException("beforeGraph");
            if (afterGraph == null) throw new ArgumentNullException("afterGraph");

            List<RelationshipGraphNode> nodes = new List<RelationshipGraphNode>();

            FindNodes(beforeGraph, afterGraph, nodes);
            FindNodes(afterGraph, beforeGraph, nodes);

            if (nodes.Count > 1)
            {
                nodes = FilterNodes(nodes);
            }

            foreach (RelationshipGraphNode node in nodes)
            {
                foreach (RelationshipGraphNode parentNode in node.ParentNodes)
                {
                    yield return parentNode.EntityToken;
                }
            }
        }

        private static void FindNodes(RelationshipGraph leftGraph, RelationshipGraph rightGraph, List<RelationshipGraphNode> foundNodes)
        {
            foreach (RelationshipGraphNode leftNode in leftGraph.TopNodes)
            {
                RelationshipGraphNode currentNode = null;

                foreach (RelationshipGraphNode rightNode in rightGraph.TopNodes)
                {
                    RelationshipGraphNode foundNode = FindNode(leftNode, rightNode, null);

                    if (foundNode != null)
                    {
                        if ((currentNode == null) || (currentNode.Level > foundNode.Level))
                        {
                            currentNode = foundNode;
                        }
                    }
                }

                if (currentNode != null)
                {
                    if (foundNodes.Find(node => node.EntityToken.GetHashCode() == currentNode.EntityToken.GetHashCode()) == null)
                    {
                        foundNodes.Add(currentNode);
                    }
                }
            }
        }



        private static RelationshipGraphNode FindNode(RelationshipGraphNode leftNode, RelationshipGraphNode rightNode, RelationshipGraphNode lastLeftNode)
        {
            if (leftNode.EntityToken.GetHashCode() != rightNode.EntityToken.GetHashCode())
            {
                if (lastLeftNode != null)
                {
                    return lastLeftNode;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if ((leftNode.ChildNode != null) && (rightNode.ChildNode != null))
                {
                    return FindNode(leftNode.ChildNode, rightNode.ChildNode, leftNode);
                }
                else
                {
                    if (lastLeftNode != null)
                    {
                        return lastLeftNode;
                    }
                    else
                    {
                        return leftNode;
                    }
                }
            }
        }



        private static List<RelationshipGraphNode> FilterNodes(List<RelationshipGraphNode> nodesToFilter)
        {
            List<RelationshipGraphNode> resultNodes = new List<RelationshipGraphNode>();

            foreach (RelationshipGraphNode nodeToFilter in nodesToFilter)
            {
                foreach (RelationshipGraphNode node in nodesToFilter)
                {
                    if (node.EntityToken.GetHashCode() != nodeToFilter.EntityToken.GetHashCode())
                    {
                        if (resultNodes.Find(n => n.EntityToken.GetHashCode() == nodeToFilter.EntityToken.GetHashCode()) == null)
                        {
                            if (IsParent(nodeToFilter, node) == false)
                            {
                                resultNodes.Add(nodeToFilter);
                            }
                        }
                    }
                }
            }

            return resultNodes;
        }



        private static bool IsParent(RelationshipGraphNode possibleChildNode, RelationshipGraphNode possibleParentNode)
        {
            foreach (RelationshipGraphNode parentNode in possibleChildNode.ParentNodes)
            {
                if (parentNode.EntityToken.GetHashCode() == possibleParentNode.EntityToken.GetHashCode())
                {
                    return true;
                }
                else
                {
                    bool result = IsParent(parentNode, possibleParentNode);

                    if (result == true)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
