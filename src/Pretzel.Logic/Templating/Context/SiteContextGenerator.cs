﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using MarkdownDeep;
using Pretzel.Logic.Extensions;
using ImportAttribute = System.ComponentModel.Composition.ImportAttribute;

namespace Pretzel.Logic.Templating.Context
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SiteContextGenerator
    {
        private static readonly Markdown Markdown = new Markdown();

        [Import] IFileSystem fileSystem;

        public SiteContext BuildContext(string path)
        {
            var context = new SiteContext
            {
                SourceFolder = path,
                OutputFolder = Path.Combine(path, "_site"),
                Posts = new List<Page>()
            };

            var postsFolder = Path.Combine(context.SourceFolder, "_posts");
            if (fileSystem.Directory.Exists(postsFolder))
            {
                foreach (var file in fileSystem.Directory.GetFiles(postsFolder, "*.*", SearchOption.AllDirectories))
                {
                    var contents = fileSystem.File.ReadAllText(file);
                    var header = contents.YamlHeader();
                    var post = new Page
                    {
                        Title = header.ContainsKey("title") ? header["title"].ToString() : "this is a post", // should this be the Site title?
                        Date = header.ContainsKey("date") ? DateTime.Parse(header["date"].ToString()) : file.Datestamp(),
                        Content = Markdown.Transform(contents.ExcludeHeader()),
                        Filepath = GetPathWithTimestamp(context.OutputFolder, file),
                        File = file,
                        Bag = header,
                    };
                    context.Posts.Add(post);
                }

                context.Posts = context.Posts.OrderByDescending(p => p.Date).ToList();
            }

            return context;
        }

        private string GetPathWithTimestamp(string outputDirectory, string file)
        {
            // TODO: detect mode from site config
            var fileName = file.Substring(file.LastIndexOf("\\"));

            var tokens = fileName.Split('-');
            var timestamp = string.Join("\\", tokens.Take(3)).Trim('\\');
            var title = string.Join("-", tokens.Skip(3));
            return System.IO.Path.Combine(outputDirectory, timestamp, title);
        }
    }
}
