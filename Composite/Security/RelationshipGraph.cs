using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Composite.Extensions;
using Composite.Security.Foundation;
using Composite.Users;


namespace Composite.Security
{
    public static class RelationshipOrientedGraphNodeExtensions
    {
        public static IEnumerable<IEnumerable<EntityToken>> GetAllPaths(this RelationshipOrientedGraphNode node)
        {
            List<List<EntityToken>> allPaths = new List<List<EntityToken>>();
            List<EntityToken> path = new List<EntityToken> { node.EntityToken };
            allPaths.Add(path);

            GetAllPathsImpl(node, path, allPaths, new List<RelationshipOrientedGraphNode>());

            return allPaths;
        }




        private static void GetAllPathsImpl(RelationshipOrientedGraphNode node, List<EntityToken> currentPath, List<List<EntityToken>> allPaths, List<RelationshipOrientedGraphNode> processedNodes)
        {
            processedNodes.Add(node);

            int count = node.Parents.Count();

            if (count == 0)
            {
                return;
            }
            else if (count == 1)
            {
                RelationshipOrientedGraphNode parentNode = node.Parents.Single();
                if (processedNodes.Contains(parentNode) == false)
                {
                    currentPath.Add(parentNode.EntityToken);

                    GetAllPathsImpl(parentNode, currentPath, allPaths, processedNodes);
                }
            }
            else
            {
                allPaths.Remove(currentPath);
                foreach (RelationshipOrientedGraphNode parentNode in node.Parents)
                {
                    if (processedNodes.Contains(parentNode) == false)
                    {
                        List<EntityToken> newCurrentPath = new List<EntityToken>(currentPath);
                        allPaths.Add(newCurrentPath);
                        newCurrentPath.Add(parentNode.EntityToken);

                        GetAllPathsImpl(parentNode, newCurrentPath, allPaths, new List<RelationshipOrientedGraphNode>(processedNodes));
                    }
                }
            }
        }
    }




    [DebuggerDisplay("EntityToken = {EntityToken}")]
    public sealed class RelationshipOrientedGraphNode
    {
        private List<RelationshipOrientedGraphNode> _parentNodes;
        private Action<RelationshipOrientedGraphNode> _expandAction;


        public RelationshipOrientedGraphNode(EntityToken entityToken, Action<RelationshipOrientedGraphNode> expandAction)
        {
            this.EntityToken = entityToken;
            _expandAction = expandAction;
        }



        public EntityToken EntityToken { get; private set; }



        public IEnumerable<RelationshipOrientedGraphNode> Parents
        {
            get
            {
                Expand();

                foreach (RelationshipOrientedGraphNode parentNode in _parentNodes)
                {
                    yield return parentNode;
                }
            }
        }



        internal void Expand()
        {
            if (_parentNodes != null) return;

            _parentNodes = new List<RelationshipOrientedGraphNode>();

            _expandAction(this);
        }



        internal void AddParant(RelationshipOrientedGraphNode parentNode)
        {
            if (_parentNodes.Contains(parentNode) == false)
            {
                _parentNodes.Add(parentNode);
            }
        }



        public override int GetHashCode()
        {
            return this.EntityToken.GetHashCode();
        }



        public override bool Equals(object obj)
        {
            return Equals(obj as RelationshipOrientedGraphNode);
        }



        public bool Equals(RelationshipOrientedGraphNode node)
        {
            if (node == null) return false;

            return node.EntityToken.Equals(this.EntityToken);
        }
    }




    public sealed class RelationshipOrientedGraph
    {
        private List<RelationshipOrientedGraphNode> _nodes = new List<RelationshipOrientedGraphNode>();


        public RelationshipOrientedGraph(EntityToken sourceEntityToken)
        {
            RelationshipOrientedGraphNode node = CreateNewNode(sourceEntityToken);

            _nodes.Add(node);
        }



        public RelationshipOrientedGraphNode Root
        {
            get
            {
                return _nodes[0];
            }
        }



        private void Expand(RelationshipOrientedGraphNode node)
        {
            IEnumerable<EntityToken> nativeParentEntityTokens = SecurityAncestorFacade.GetParents(node.EntityToken);
            if (nativeParentEntityTokens != null)
            {
                nativeParentEntityTokens.ForEach(f => AddEntityToken(node, f));
            }


            IEnumerable<EntityToken> auxiliaryParentEntityTokens = AuxiliarySecurityAncestorFacade.GetParents(node.EntityToken);
            if (auxiliaryParentEntityTokens != null)
            {
                auxiliaryParentEntityTokens.ForEach(f => AddEntityToken(node, f));
            }


            IEnumerable<EntityToken> hookingParentEntityTokens = HookingFacade.GetHookies(node.EntityToken);
            if (hookingParentEntityTokens != null)
            {
                hookingParentEntityTokens.ForEach(f => AddEntityToken(node, f));
            }
        }



        private void AddEntityToken(RelationshipOrientedGraphNode node, EntityToken parentEntityToken)
        {
            RelationshipOrientedGraphNode existingParentNode =
                (from n in _nodes
                 where n.EntityToken.Equals(parentEntityToken)
                 select n).SingleOrDefault();

            if (existingParentNode != null)
            {
                node.AddParant(existingParentNode);
            }
            else
            {
                RelationshipOrientedGraphNode parentNode = CreateNewNode(parentEntityToken);
                _nodes.Add(parentNode);

                node.AddParant(parentNode);
            }
        }



        private RelationshipOrientedGraphNode CreateNewNode(EntityToken entityToken)
        {
            RelationshipOrientedGraphNode node = new RelationshipOrientedGraphNode(entityToken, Expand);

            return node;
        }
    }












    public enum RelationshipGraphSearchOption
    {
        Native,
        Hooked,
        Both
    }



    public sealed class RelationshipGraph
    {
        private RelationshipGraphSearchOption _searchOption;

        private Dictionary<int, List<RelationshipGraphNode>> _levels = new Dictionary<int, List<RelationshipGraphNode>>();
        private HashSet<EntityToken> _visitedEntityTokens = new HashSet<EntityToken>();
        private bool _moreLevelsToExpend;

        public RelationshipGraph(EntityToken sourceEntityToken, RelationshipGraphSearchOption searchOption)
            : this(sourceEntityToken, searchOption, false)
        {
        }


        public RelationshipGraph(EntityToken sourceEntityToken, RelationshipGraphSearchOption searchOption, bool lazyEvaluation)
        {
            if (sourceEntityToken == null) throw new ArgumentNullException("sourceEntityToken");

            _searchOption = searchOption;

            RelationshipGraphNode node = new RelationshipGraphNode(sourceEntityToken, 0, RelationshipGraphNodeType.Entity);
            _levels.Add(0, new List<RelationshipGraphNode> { node });

            string userName = UserValidationFacade.IsLoggedIn() ? UserSettings.Username : null;

            ExpandNextLevel(userName);

            if (lazyEvaluation == false)
            {
                while (_moreLevelsToExpend == true)
                {
                    ExpandNextLevel(userName);
                }
            }
        }



        public IEnumerable<RelationshipGraphLevel> Levels
        {
            get
            {
                return new RelationshipGraphLevelEnumerable(this);
            }
        }



        public IEnumerable<RelationshipGraphNode> TopNodes
        {
            get
            {
                foreach (List<RelationshipGraphNode> nodes in _levels.Values)
                {
                    foreach (RelationshipGraphNode node in nodes)
                    {
                        if (node.ParentNodes.Count == 0)
                        {
                            yield return node;
                        }
                    }
                }
            }
        }



        public IEnumerable<RelationshipGraphNode> BottomNodes
        {
            get
            {
                foreach (List<RelationshipGraphNode> nodes in _levels.Values)
                {
                    foreach (RelationshipGraphNode node in nodes)
                    {
                        if (node.ChildNode == null)
                        {
                            yield return node;
                        }
                    }
                }
            }
        }



        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (RelationshipGraphLevel level in this.Levels)
            {
                sb.AppendLine("Level: " + level.Level);
                foreach (EntityToken entityToken in level.Entities)
                {
                    sb.AppendLine("Native: Type = " + entityToken.Type + " Source = " + entityToken.Source + " Id = " + entityToken.Id);
                }

                foreach (EntityToken entityToken in level.HookedEntities)
                {
                    sb.AppendLine("Hooked: Type = " + entityToken.Type + " Source = " + entityToken.Source + " Id = " + entityToken.Id);
                }

                sb.AppendLine("---------");
            }

            return sb.ToString();
        }


        internal int LevelCount
        {
            get
            {
                return _levels.Count;
            }
        }



        internal RelationshipGraphLevel GetLevel(int level)
        {
            string userName = UserValidationFacade.IsLoggedIn() ? UserSettings.Username : null;

            while ((_levels.Count - 1 < level) && (_moreLevelsToExpend == true))
            {
                ExpandNextLevel(userName);
            }

            if (_levels.Count - 1 < level)
            {
                return null;
            }

            return new RelationshipGraphLevel(level, _levels[level]);
        }



        private void ExpandNextLevel(string userName)
        {
            int levelNumber = _levels.Count - 1;

            if (levelNumber > 1000)
            {
                throw new InvalidOperationException(string.Format("The entity token '{0}' has more than 1000 levels of parents, this might be a infinit loop", _levels[0][0].EntityToken));
            }

            _moreLevelsToExpend = false;

            List<RelationshipGraphNode> nodes = _levels[levelNumber];

            foreach (RelationshipGraphNode node in nodes)
            {
                if ((_searchOption == RelationshipGraphSearchOption.Native) || (_searchOption == RelationshipGraphSearchOption.Both))
                {
                    IEnumerable<EntityToken> parentEntityTokens;
                    if (EntityTokenCacheFacade.GetCachedNativeParents(node.EntityToken, out parentEntityTokens, userName) == false)                    
                    {
                        parentEntityTokens = SecurityAncestorFacade.GetParents(node.EntityToken);

                        EntityTokenCacheFacade.AddNativeCache(node.EntityToken, parentEntityTokens);
                    }

                    if (parentEntityTokens != null)
                    {
                        AddNewParentEntityTokens(node, parentEntityTokens, RelationshipGraphNodeType.Entity, levelNumber);
                    }
                }

                if ((_searchOption == RelationshipGraphSearchOption.Hooked) || (_searchOption == RelationshipGraphSearchOption.Both))
                {
                    IEnumerable<EntityToken> parentEntityTokens;

                    if (EntityTokenCacheFacade.GetCachedHookingParents(node.EntityToken, out parentEntityTokens, userName) == false)
                    {
                        IEnumerable<EntityToken> auxiliaryParentEntityTokens = AuxiliarySecurityAncestorFacade.GetParents(node.EntityToken);
                        IEnumerable<EntityToken> hookingParentEntityTokens = HookingFacade.GetHookies(node.EntityToken);

                        if (auxiliaryParentEntityTokens == null)
                        {
                            parentEntityTokens = hookingParentEntityTokens;
                        }
                        else
                        {
                            parentEntityTokens = auxiliaryParentEntityTokens;

                            if (hookingParentEntityTokens != null)
                            {
                                parentEntityTokens = parentEntityTokens.Concat(hookingParentEntityTokens);
                            }
                        }

                        EntityTokenCacheFacade.AddHookingCache(node.EntityToken, parentEntityTokens);
                    }

                    if (parentEntityTokens != null)
                    {
                        AddNewParentEntityTokens(node, parentEntityTokens, RelationshipGraphNodeType.Hooking, levelNumber);
                    }                        
                }
            }
        }



        private void AddNewParentEntityTokens(RelationshipGraphNode childNode, IEnumerable<EntityToken> parents, RelationshipGraphNodeType nodeType, int levelNumber)
        {
            int newLevelNumber = levelNumber + 1;

            List<RelationshipGraphNode> levelNodes;
            if (_levels.TryGetValue(newLevelNumber, out levelNodes) == false)
            {
                levelNodes = new List<RelationshipGraphNode>();
                _levels.Add(newLevelNumber, levelNodes);
            }


            foreach (EntityToken parent in parents)
            {
                if (_visitedEntityTokens.Contains(parent)) continue; // We have already visisted this entity token, no new information here
                _visitedEntityTokens.Add(parent);

                RelationshipGraphNode parentNode = new RelationshipGraphNode(parent, newLevelNumber, nodeType);

                levelNodes.Add(parentNode);

                childNode.ParentNodes.Add(parentNode);
                parentNode.ChildNode = childNode;

                _moreLevelsToExpend = true;
            }
        }
    }
}
