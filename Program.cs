using System;
using System.IO;
using System.Linq;
using Amaze.Parse;
using Amaze.Solve;

namespace Amaze
{
	internal class Program
	{
		public static void Main (string[] args)
		{
			if (args.Length <= 0) {
				Console.WriteLine ("usage: amaze {path|directory}.");
				return;
			}

			var path = args [0];
			var optimized = args.Length >= 2 && args [1] == "--optimized";

			if (SolveAllLevels (path, optimized)) {
				return;
			}

			SolveLevel (path, optimized);
		}


		private static bool SolveAllLevels (string fileDirectory, bool optimized)
		{
			if (!Directory.Exists (fileDirectory)) {
				return false;
			}

			var filePaths = Directory.GetFiles (fileDirectory, "*.xml", SearchOption.TopDirectoryOnly);
			foreach (var filePath in filePaths) {
				SolveLevel (filePath, optimized);
			}

			return true;
		}

		private static void SolveLevel (string filePath, bool optimized)
		{
			if (!File.Exists (filePath)) {
				Console.WriteLine ($"level data in path [{filePath}] NOT found!");
				return;
			}

			var startTime = DateTime.Now;

			// parse data
			var data = Parser.Parse (filePath);
			if (data == null) {
				Console.WriteLine ($"level data in path [{filePath}] can NOT be parsed!");
				return;
			}

			var solver = new Solver (data, optimized);
			RunSolver (solver);

			var time = DateTime.Now - startTime;

			var fileName = Path.GetFileName (filePath);
#if DEBUG
			OutputSolutions (fileName, time, solver);
#else
			OutputSolution (fileName, time, solver);
#endif
		}

		private static void RunSolver (Solver solver)
		{
			Debug.Log ("SOLVE INIT ----------");
			Debug.Log (solver);

			Debug.Log ("SOLVE START ---------");
			solver.Start ();

			while (!solver.IsEnded) {
				solver.Tick ();
			}

			Debug.Log ("SOLVE END -----------");

#if DEBUG
			solver.OutputSolutionPathSteams ();
#endif
		}

		private static void OutputSolution (string fileName, TimeSpan time, Solver solver)
		{
			var outputPrefix = $"{fileName}, {time.TotalSeconds}";

			var solution = solver.OutputShortestSolution ();
			if (solution == null || solution.Length <= 0) {
				outputPrefix += ", no solution.";
				Console.WriteLine (outputPrefix);
				return;
			}

			var output = solution.Aggregate (outputPrefix, (current, step) => current + (", " + step));
			Console.WriteLine (output);
		}

		private static void OutputSolutions (string fileName, TimeSpan time, Solver solver)
		{
			var outputPrefix = $"{fileName}, {time.TotalSeconds}";

			var solutions = solver.OutputSolutions ();
			if (solutions == null || solutions.Count <= 0) {
				Console.WriteLine (outputPrefix + ", no solution.");
				return;
			}

			foreach (var solution in solutions) {
				var output = solution.Aggregate (outputPrefix, (current, step) => current + (", " + step));
				Console.WriteLine (output);
			}
		}
	}
}