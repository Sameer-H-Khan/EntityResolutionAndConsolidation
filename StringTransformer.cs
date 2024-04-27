using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Microsoft.ProgramSynthesis.Transformation.Text;
using Microsoft.ProgramSynthesis.Wrangling;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;

namespace EntityResolutionAndConsolidation
{
    internal class StringTransformer
    {
        private static void Main(string[] args)
        {
            string filePath = "C:\\Users\\ryanp\\source\\repos\\EntityResolutionAndConsolidation\\EntityResolutionAndConsolidation\\book-snippet.tsv";
            int identifierIndex = 1; // Index for the ISBN or any identifier
            int[] fieldsOfInterestIndices = { 2, 3 }; // Indices of the fields of interest, e.g., title and author

            var dataByIdentifier = new Dictionary<string, List<string[]>>();

            // Read through the file and add all columns to a list of string arrays
            using (StreamReader reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] fields = line.Split('\t');
                    string? identifier = fields.Length > identifierIndex ? fields[identifierIndex] : null;

                    List<string> entries = new();
                    foreach (var index in fieldsOfInterestIndices)
                    {
                        if (index < fields.Length)
                            entries.Add(fields[index]);
                        else
                            entries.Add(""); // Handle missing data
                    }

                    if (!dataByIdentifier.ContainsKey(identifier))
                    {
                        dataByIdentifier[identifier] = new List<string[]>();
                    }
                    dataByIdentifier[identifier].Add(entries.ToArray());
                }
            }

            // Group individual features by another feature (in book's case we are grouping author and title by ISBN)
            foreach (var group in dataByIdentifier)
            {
                Console.WriteLine($"Processing Identifier: {group.Key}");

                for (int i = 0; i < fieldsOfInterestIndices.Length; i++)
                {
                    List<string> values = new List<string>();
                    foreach (var entry in group.Value)
                    {
                        values.Add(entry[i]);
                    }

                    string[,] permutations = CandidateReplacementPermutations(values.ToArray());
                    PrintMatrix(permutations, $"Permutations for Field Index {fieldsOfInterestIndices[i]}");
                }
            }

            // Transformation map: Program -> List of Transformations
            Dictionary<string, List<string>> transformationMap = new();

            // Process each identifier group
            foreach (var group in dataByIdentifier)
            {
                Console.WriteLine($"Processing Identifier: {group.Key}");

                for (int i = 0; i < fieldsOfInterestIndices.Length; i++)
                {
                    List<string> values = new List<string>();
                    foreach (var entry in group.Value)
                    {
                        values.Add(entry[i]);
                    }

                    string[,] permutations = CandidateReplacementPermutations(values.ToArray());
                    foreach (var transformation in GetTransformations(permutations))
                    {
                        //Some titles are exact matches with the author being slightly different
                        //These transformations will be empty strings
                        //TODO: Change candidate replacement permutations to just be a set
                        //TODO: Also change this to just do one direction?
                        if(transformation == "")
                        {
                            continue;
                        }
                        string program = LearnTransformation(transformation);
                        if (!transformationMap.ContainsKey(program))
                        {
                            transformationMap[program] = new List<string>();
                        }
                        transformationMap[program].Add(transformation);
                    }
                }
            }

            // Print the transformation map
            foreach (var entry in transformationMap)
            {
                Console.WriteLine($"Program: {entry.Key}");
                foreach (var transformation in entry.Value)
                {
                    Console.WriteLine($"  Transformation: {transformation}");
                }
            }
        }

        private static IEnumerable<string> GetTransformations(string[,] permutations)
        {
            for (int i = 0; i < permutations.GetLength(0); i++)
            {
                for (int j = 0; j < permutations.GetLength(1); j++)
                {
                    if (i != j && !string.IsNullOrEmpty(permutations[i, j]))
                        yield return permutations[i, j];
                }
            }
        }

        /*
         * Given a string in the format lhs->rhs where lhs and rhs are logically the same, but in
         * different formats, this method will return a proposed string transformation program using
         * the PROSE API
         */
        private static string LearnTransformation(string transformation)
        {
            string[] lhsrhs= transformation.Split(" -> ");
            string lhs = lhsrhs[0];
            string rhs = lhsrhs[1];

            Session session = new();

            session.Constraints.Add(
                new Example(new InputRow(lhs), rhs)
            );

            Program topRankedProgram = session.Learn();

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn format name program.");
                return "This did not work";
            }
            else
            {
                return topRankedProgram.ToString();
            }
        }

        /*
        * Given a list of strings believed to be the same value in a different format,
        * return a list of all the combinations of string replacing another in the format
        * str1 -> str2
        */
        public static string[,] CandidateReplacementPermutations(string[] candidateCluster)
        {
            int numOfCandidates = candidateCluster.Length;
            string[,] replacements = new string[numOfCandidates, numOfCandidates];
            for (int i = 0; i < numOfCandidates; i++)
            {
                for (int j = 0; j < numOfCandidates; j++)
                {
                    if (i == j)
                    {
                        replacements[i, j] = "";
                    }
                    else if (candidateCluster[i] == candidateCluster[j])
                    {
                        replacements[i, j] = "";
                    }
                    else
                    {
                        replacements[i, j] = candidateCluster[i] + " -> " + candidateCluster[j];
                    }
                }
            }

            return replacements;
        }

        private static void PrintMatrix(string[,] matrix, string label)
        {
            Console.WriteLine(label);
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    if (i != j)
                        Console.WriteLine(matrix[i, j]);
                }
            }
        }
    }
}
