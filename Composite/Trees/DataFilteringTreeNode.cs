﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Composite.Linq;
using System.Collections.Generic;


namespace Composite.Trees
{
    public abstract class DataFilteringTreeNode : TreeNode
    {
        internal abstract Type CurrentDataInterfaceType { get; }



        internal virtual Expression CreateFilterExpression(ParameterExpression parameterExpression, TreeNodeDynamicContext dynamicContext, IEnumerable<int> filtersToSkip = null)
        {
            Expression expression = null;

            foreach (FilterNode filterNode in FilterNodes)
            {
                if ((filtersToSkip != null) && (filtersToSkip.Contains(filterNode.Id)) == true) continue;

                Expression filterExpression = null;
                if (dynamicContext.Direction == TreeNodeDynamicContextDirection.Down)
                {
                    filterExpression = filterNode.CreateDownwardsFilterExpression(parameterExpression, dynamicContext);
                }
                else
                {
                    filterExpression = filterNode.CreateUpwardsFilterExpression(parameterExpression, dynamicContext);
                }

                if (filterExpression == null) continue;

                expression = expression.NestedAnd(filterExpression);
            }

            return expression;
        }



        internal Expression CreateAccumulatedFilterExpression(ParameterExpression parameterExpression, Type affectedInterfaceType, TreeNodeDynamicContext dynamicContext, IEnumerable<int> filtersToSkip = null)
        {
            TreeNode treeNode = this;

            Expression currentExpression = null;
            while (treeNode != null)
            {
                DataFilteringTreeNode dataFilteringTreeNode = treeNode as DataFilteringTreeNode;

                if ((dataFilteringTreeNode != null) &&
                    ((dataFilteringTreeNode == this) || ((dataFilteringTreeNode is DataFolderElementsTreeNode) == true)) &&
                    (dataFilteringTreeNode.CurrentDataInterfaceType == affectedInterfaceType))
                {
                    Expression filterExpression = dataFilteringTreeNode.CreateFilterExpression(parameterExpression, dynamicContext, filtersToSkip);

                    if (filterExpression != null)
                    {
                        currentExpression = currentExpression.NestedAnd(filterExpression);
                    }
                }
                else
                {
                    break;
                }

                treeNode = treeNode.ParentNode;
            }

            //currentExpression.DebugLogExpression("DataFileringTreeNode", "Accumulated Filter Expression");

            return currentExpression;
        }



        protected Expression CreateAccumulatedOrderByExpression(Expression sourceExpression, ParameterExpression parameterExpression)
        {
            Expression resultExpression = sourceExpression;
            bool isFirst = true;
            foreach (OrderByNode orderByNode in this.OrderByNodes)
            {
                resultExpression = orderByNode.CreateOrderByExpression(resultExpression, parameterExpression, isFirst);
                isFirst = false;
            }

            return resultExpression;
        }
    }
}
