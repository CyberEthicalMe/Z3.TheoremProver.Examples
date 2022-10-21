using Microsoft.Z3;

namespace Z3.Solver.Examples
{
    internal class Example
    {
        /// <summary>
        /// (x > 2, y < 10, x + 2*y == 7)
        /// </summary>
        public static void SimpleConstraints()
        {
            Console.WriteLine("(x > 2, y < 10, x + 2*y == 7)");
            using (var ctx = new Context())
            {
                // create Z3 variables
                var x = (IntExpr)ctx.MkConst(ctx.MkSymbol("x"), ctx.IntSort);
                var y = (IntExpr)ctx.MkConst(ctx.MkSymbol("y"), ctx.IntSort);

                var vars = new[] { x, y };

                // x > 2
                var c1 = ctx.MkGt(x, ctx.MkInt(2));

                // y < 10
                var c2 = ctx.MkLt(y, ctx.MkInt(10));

                //  x + 2*y == 7
                var c3 = ctx.MkEq(x + 2 * y, ctx.MkInt(7));

                // Join constraints, all have to be satisfied
                var constraints = ctx.MkAnd(c1, c2, c3);

                // Create solver
                var solver = ctx.MkSolver();

                // Apply constraints
                solver.Assert(constraints);

                // Custom method to print many solutions
                FindSolves(ctx, solver,
                    (m) => vars.Select(exp => m.Evaluate(exp)).ToArray(),
                    (exprs) => exprs.Select((expr, i) => ctx.MkNot(ctx.MkEq(vars[i], expr))).ToArray());

                // Suggested to do so in official source code
                ctx.Dispose();
            }
        }

        /// <summary>
        /// Prints solutions until exited or contraints are no longer solvable
        /// </summary>
        private static void FindSolves(Context ctx, Microsoft.Z3.Solver solver, Func<Model, Expr[]> evaluator, Func<Expr[], BoolExpr[]> getNewConstraints)
        {
            // Check if given constraints can produce solutions
            if (solver.Check() != Status.SATISFIABLE)
            {
                Console.WriteLine(solver.Check());
                return;
            }

            Expr[] solves = evaluator.Invoke(solver.Model);

            Console.WriteLine(string.Join(", ", solves.Select(s => s)));

            BoolExpr[] newConstraints = getNewConstraints(solves);

            var newConstraint = ctx.MkAnd(newConstraints);

            // Exclude current solution from space
            solver.Assert(newConstraint);
            Console.Write("Show next? [space]\r");

            var keyInfo = Console.ReadKey().Key;
            Console.Write("                  \r");

            if (keyInfo == ConsoleKey.Spacebar)
            {
                FindSolves(ctx, solver, evaluator, getNewConstraints);
            }
        }
    }
}
