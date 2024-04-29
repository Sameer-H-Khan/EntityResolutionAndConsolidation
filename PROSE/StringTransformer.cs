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
            string filePath = "C:\\Users\\ryanp\\source\\repos\\EntityResolutionAndConsolidation\\EntityResolutionAndConsolidation\\random250Sample.tsv";
            int identifierIndex = 1; // Index for the ISBN or any identifier
            int[] fieldsOfInterestIndices = { 2, 3 }; // Indices of the fields of interest, e.g., title and author

            var clusterByIdentifier = new Dictionary<string, List<string[]>>();

            // Read through the file and add all feature columns to a list of string arrays
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

                    if (!clusterByIdentifier.ContainsKey(identifier))
                    {
                        clusterByIdentifier[identifier] = new List<string[]>();
                    }
                    clusterByIdentifier[identifier].Add(entries.ToArray());
                }
            }

            // Group individual features by another feature (in book's case we are grouping author and title by ISBN)
            foreach (var group in clusterByIdentifier)
            {
                Console.WriteLine($"Processing Identifier has length: {group.Value.Count}");
                Console.WriteLine($"Processing Identifier: {group.Key}");

                for (int i = 0; i < fieldsOfInterestIndices.Length; i++)
                {
                    List<string> values = new List<string>();
                    foreach (var entry in group.Value)
                    {
                        values.Add(entry[i]);
                    }

                    HashSet<string> permutations = CandidateReplacementPermutations(values.ToArray());
                    foreach (string permutation in permutations)
                    {
                        Console.WriteLine(permutation.ToString());
                    }
                }
            }

            // Transformation map: PROSE Program ==> candidate transformation
            List<string> transformationMap = new();

            // Process each identifier group giving the best PROSE program for each element to each other element within the cluster
            foreach (var group in clusterByIdentifier)
            {
                Console.WriteLine($"Processing Identifier: {group.Key}");

                for (int i = 0; i < fieldsOfInterestIndices.Length; i++)
                {
                    List<string> values = new List<string>();
                    foreach (var entry in group.Value)
                    {
                        values.Add(entry[i]);
                    }

                    HashSet<string> permutations = CandidateReplacementPermutations(values.ToArray());
                    foreach (string transformation in permutations)
                    {
                        Console.WriteLine($"Learning Transformation for : {transformation}");
                        string program = LearnTransformation(transformation);
                        string mappedProgram = program + "==>" + transformation;
                        transformationMap.Add(mappedProgram);
                    }
                }
            }

            string outputPath = "C:\\Users\\ryanp\\source\\repos\\EntityResolutionAndConsolidation\\EntityResolutionAndConsolidation\\programs_one_to_one.txt";
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                foreach (string entry in transformationMap)
                {
                    writer.WriteLine(entry);
                }
            }
        }

        private static string LearnTransformation(string transformation)
        {
            string[] lhsrhs= transformation.Split("->");
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
        public static HashSet<string> CandidateReplacementPermutations(string[] candidateCluster)
        {
            int numOfCandidates = candidateCluster.Length;
            HashSet<string> replacements = new();
            for (int i = 0; i < numOfCandidates; i++)
            {
                for (int j = 0; j < numOfCandidates; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    else if (candidateCluster[i] == candidateCluster[j])
                    {
                        continue;
                    }
                    else
                    {
                        replacements.Add(candidateCluster[i] + "->" + candidateCluster[j]);
                    }
                }
            }

            return replacements;
        }
    }
}