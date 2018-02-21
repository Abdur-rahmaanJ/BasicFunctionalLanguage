﻿using FBL.Parsing.Nodes;
using System;
using System.Collections.Generic;

namespace FBL.Interpretation
{
    public partial class Interpreter
    {
        private Context globalContext = new Context(null);
        private List<IModule> loadedModules = new List<IModule>();


        public ExpressionNode Run(CoreNode program)
        {
            var result = new ExpressionNode();

            foreach (var exp in program.code)
                result = Evaluate((dynamic)exp, GetGlobalContext());

            return result;
        }

        public ExpressionNode Run(ExpressionNode node, StringNode data)
        {
            return Evaluate(new FunctionCallNode
            {
                Argument = data,
                CalleeExpression = node,
                Context = globalContext
            }, globalContext);
        }

        public ExpressionNode Run(string name, ExpressionNode args)
        {
            if (!globalContext.Values.TryGetValue(name, out ExpressionNode node))
                return new ExpressionNode();

            if (!(node is FunctionNode f))
                return new ExpressionNode();

            return Evaluate(new FunctionCallNode
            {
                Argument = args,
                CalleeExpression = f,
                Context = globalContext
            }, globalContext);
        }


        public void AddModule(IModule module)
        {
            module.OnLoad(this);
            loadedModules.Add(module);
        }

        public void SetVariable(string name, ExpressionNode value, Context context)
        {
            context.Values.Add(name, value);
        }

        public ExpressionNode ChangeValue(ExpressionNode from, ExpressionNode to, Context context)
        {
            if (context == null) return null;

            var ctx = context;
            while (ctx != null)
            {
                foreach (var v in ctx.Values)
                {
                    if (v.Value.Value == from)
                    {
                        ctx.Values[v.Key] = to;
                        return to;
                    }
                }
                ctx = ctx.Parent;
            }

            return new ExpressionNode();
        }

        public Context GetGlobalContext() => globalContext;

        private ExpressionNode Evaluate(FunctionCallNode node, Context context)
        {
            ExpressionNode left = Evaluate((dynamic)node.CalleeExpression, context);

            if (left is FunctionNode left_func)
            {
                ExpressionNode right = Evaluate((dynamic)node.Argument, context);

                if (left_func.IsNative)
                    return left_func.Function(right, context);

                var sub_context = new Context(left_func.Context);
                sub_context.Values.Add(left_func.Parameter?.Name ?? "it", right);
                return Evaluate((dynamic)left_func.Code, sub_context);
            }
            
            throw new InvalidOperationException(
                $"calling '{node.CalleeExpression?.GetType().Name ?? "null"}'\n" +
                $"  evaluated to: {left?.ToString() ?? "null"}\n" +
                $"is impossible");
        }

        private ExpressionNode Evaluate(BlockNode node, Context context)
        {
            ExpressionNode result = new ExpressionNode() { Context = context };
            
            foreach (var exp in node.Code)
                result = Evaluate((dynamic)exp, context);
            
            return result;
        }

        private ExpressionNode Evaluate(VariableNode node, Context context)
        {
            var ctx = context;
            while (ctx != null)
            {
                if (ctx.Values.TryGetValue(node.Name, out ExpressionNode value))
                {
                    // value = value ?? new ExpressionNode();
                    // value.Context = ctx;
                    return value;
                }
                ctx = ctx.Parent;
            }

            var expression = new ExpressionNode();
            context.Values.Add(node.Name, expression);
            expression.Context = context;
            return expression;
        }

        private ExpressionNode Evaluate(ExpressionNode node, Context context)
        {
            var value = node ?? new ExpressionNode();
            value.Context = value.Context ?? context;
            return value;
        }
    }
}
