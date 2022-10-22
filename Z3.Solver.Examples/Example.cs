using Microsoft.Z3;
using System.Linq;

namespace Z3.Solver.Examples
{
    internal class Example
    {
        /// <summary>
        /// (x > 2, y < 10, x + 2*y == 7)
        /// </summary>
        public static void SimpleConstraints()
        {
            Console.WriteLine($"\n**{nameof(SimpleConstraints)}**\n");
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

        public static void SmallSudoku()
        {
            Console.WriteLine($"\n**{nameof(SmallSudoku)}**\n");
            /*   X ->
             * Y [ y0x0, y0x1, y0x2 ]
             * | [ y1x0, y1x1, y1x2 ]
             *   [ y2x0, y2x1, y2x2 ]
             */
            using (var ctx = new Context())
            {
                // Define grid
                var gridVars = new IntExpr[3][];

                for (var y = 0; y < gridVars.Length; y++)
                {
                    gridVars[y] = new IntExpr[3];
                    for (var x = 0; x < 3; x++)
                    {
                        gridVars[y][x] = (IntExpr)ctx.MkConst(ctx.MkSymbol($"y{y}x{x}"), ctx.IntSort);
                    }
                }

                // Define constraint: each cell of grid contains distinct numbers 
                var c1 = ctx.MkDistinct(gridVars.SelectMany(row => row));

                // Define constraint: each cell contains values from range [1,9]
                var c2 = ctx.MkAnd(gridVars.SelectMany(row => row)
                    .Select(var => ctx.MkAnd(
                        ctx.MkGe(ctx.MkInt(9), var),
                        ctx.MkGe(var, ctx.MkInt(1))
                    ))
                );

                // Join constraints, all have to be satisfied
                var constraints = ctx.MkAnd(c1, c2);

                // Create solver
                var solver = ctx.MkSolver();

                // Apply constraints
                solver.Assert(constraints);

                // Custom method to print many solutions
                FindSolves(ctx, solver,
                    (m) => gridVars.SelectMany(row => row).Select(exp => m.Evaluate(exp)).ToArray(),
                    (exprs) => exprs.Select((expr, i) => ctx.MkNot(ctx.MkEq(gridVars[i / 3][i % 3], expr))).ToArray(),
                    (solves) => Console.WriteLine(
                        $"[{string.Join(",",solves.Take(3))}]\n" +
                        $"[{string.Join(",", solves.Skip(3).Take(3))}]\n"+
                        $"[{string.Join(",", solves.Skip(6).Take(3))}]\n"));

                ctx.Dispose();
            }

        }

        /// <summary>
        /// Prints solutions until exited or contraints are no longer solvable
        /// </summary>
        private static void FindSolves(Context ctx, Microsoft.Z3.Solver solver, Func<Model, Expr[]> evaluator, Func<Expr[], BoolExpr[]> getNewConstraints, Action<Expr[]> printer = null)
        {
            // Check if given constraints can produce solutions
            if (solver.Check() != Status.SATISFIABLE)
            {
                Console.WriteLine(solver.Check());
                return;
            }

            Expr[] solves = evaluator.Invoke(solver.Model);

            if(printer == null)
            {
                Console.WriteLine(string.Join(", ", solves.Select(s => s)));
            }
            else
            {
                printer.Invoke(solves);
            }

            BoolExpr[] newConstraints = getNewConstraints(solves);

            var newConstraint = ctx.MkAnd(newConstraints);

            // Exclude current solution from space
            solver.Assert(newConstraint);
            Console.Write("Show next? [space]\r");

            var keyInfo = Console.ReadKey().Key;
            Console.Write("                  \r");

            if (keyInfo == ConsoleKey.Spacebar)
            {
                FindSolves(ctx, solver, evaluator, getNewConstraints, printer);
            }
        }
    }
}
