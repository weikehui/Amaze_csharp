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
				Debug.Log ("usage: amaze {path|directory}.");
				return;
			}

			if (SolveAllLevels (args [0])) {
				return;
			}

			SolveLevel (args [0]);
		}


		private static bool SolveAllLevels (string fileDirectory)
		{
			if (!Directory.Exists (fileDirectory)) {
				return false;
			}

			var filePaths = Directory.GetFiles (fileDirectory, "*.xml", SearchOption.TopDirectoryOnly);
			foreach (var filePath in filePaths) {
				SolveLevel (filePath);
			}

			return true;
		}

		private static void SolveLevel (string filePath)
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

			var solver = new Solver (data);
			RunSolver (solver);

			var duration = DateTime.Now - startTime;

			var fileName = Path.GetFileName (filePath);
			var output = $"{fileName}, {duration.TotalSeconds}";

			var solution = solver.OutputSolution ();
			if (solution == null || solution.Length <= 0) {
				output += ", no solution.";
				Console.WriteLine (output);
				return;
			}

			output = solution.Aggregate (output, (current, step) => current + (", " + step));
			Console.WriteLine (output);
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
		}
	}
}