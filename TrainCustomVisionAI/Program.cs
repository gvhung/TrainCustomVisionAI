using Microsoft.Cognitive.CustomVision;
using Microsoft.Cognitive.CustomVision.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrainCustomVisionAI
{
    class Program
    {
        static int tagCount = 0;
        static void Main(string[] args)
        {
            try
            {
                CreateTestTrainSet();
                Console.WriteLine("Train and test data created");
                Train();
                Console.WriteLine("Training complete...");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private static void Train()
        {
            string trainingKey = "fdf8998652a44b5ea1380439f25d71ca";
            string projectKey = "INat-ImageClassifier";
            string trainPath = @"c:\data\Images\train\";

            TrainingApiCredentials trainingCredentials = new TrainingApiCredentials(trainingKey);
            TrainingApi trainingApi = new TrainingApi(trainingCredentials);

            var project = trainingApi.CreateProject(projectKey);
            var files = new DirectoryInfo(trainPath).GetFiles("*", SearchOption.AllDirectories);

            foreach (var image in files)
            {
                if (tagCount >= 50)
                {
                    break;
                }
                List<string> tags = new List<string>();

                string tagName = image.Directory.Name.Replace("_", " ");
                var tagdata = trainingApi.GetTags(project.Id);
                CreateTags(trainingApi, project, tags, tagName, tagdata);
                var imagestream = new MemoryStream(File.ReadAllBytes(image.FullName));

                Console.WriteLine("Sending image " + image.Name +  " To Custom Vision AI for training...");
                trainingApi.CreateImagesFromData(project.Id, imagestream, tags);
            }

            var iteration = trainingApi.TrainProject(project.Id);

            while (iteration.Status == "Training")
            {
                Thread.Sleep(100);
                iteration = trainingApi.GetIteration(project.Id, iteration.Id);
                Console.WriteLine("Training...");
            }

            iteration.IsDefault = true;
            trainingApi.UpdateIteration(project.Id, iteration.Id, iteration);
        }

        private static void CreateTags(TrainingApi trainingApi, ProjectModel project,
            List<string> tags, string tagName, ImageTagListModel tagdata)
        {
            bool foundtag = false;

            foreach (var tag in tagdata.Tags)
            {

                if (tag.Name == tagName)
                {
                    tags.Add(tag.Id.ToString());
                    foundtag = true;
                    break;
                }
            }
            if (!foundtag)
            {
                var tagId = trainingApi.CreateTag(project.Id, tagName);
                tagCount++;
                tags.Add(tagId.Id.ToString());
            }
        }

        private static void CreateTestTrainSet()
        {
            string imageDatasetPath = @"c:\data\Images\";
            string trainPath = @"c:\data\Images\train\";
            string testPath = @"c:\data\Images\test\";

            var genusDirectories = new DirectoryInfo(imageDatasetPath).GetDirectories();
            foreach (var d in genusDirectories)
            {
                var subdirectories = d.GetDirectories();
                foreach (var s in subdirectories)
                {
                    var files = s.GetFiles();

                    List<FileInfo> trainFiles = null;
                    List<FileInfo> testFiles = null;

                    if (files.Length > 20)
                    {
                        trainFiles = files.Take(20).ToList();
                        testFiles = files.Skip(20).Take(20).ToList();
                    }
                    else
                    {
                        int length = files.Length;
                        int count = Convert.ToInt32((files.Length) * 0.6);
                        if (count < 5)
                        {
                            count = 5;
                        }

                        int testcount = (files.Length) - count;
                        trainFiles = files.Take(count).ToList();
                        testFiles = files.Skip(count).Take(testcount).ToList();

                        if (trainFiles.Count < 5)
                        {
                            trainFiles.Clear();
                            testFiles.Clear();
                        }

                    }

                    CopyFiles(trainFiles, trainPath);
                    CopyFiles(testFiles, testPath);

                }
            }
        }

        private static void CopyFiles(List<FileInfo> files, string path)
        {
            foreach (var fi in files)
            {
                string newDirectory = string.Concat(path, fi.Directory.Parent.Name, "\\", fi.Directory.Name, "\\");
                if (!Directory.Exists(newDirectory))
                {
                    Directory.CreateDirectory(newDirectory);
                }
                fi.CopyTo(newDirectory + fi.Name, true);
            }
        }
    }
}
