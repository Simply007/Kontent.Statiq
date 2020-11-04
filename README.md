# Kontent.Statiq

![CI](https://github.com/alanta/Kontent.Statiq/workflows/CI/badge.svg?branch=master)
[![NuGet](https://img.shields.io/nuget/v/Kontent.Statiq.svg)](https://www.nuget.org/packages/Kontent.Statiq)

Module to retrieve content from [Kentico Kontent](https://kontent.ai) for building static websites with [Statiq](https://Statiq.dev).

## Getting started

* Setup a project on [Kontent](https://app.kontent.ai/sign-up), there's a free [Developer Plan](https://kontent.ai/pricing#developer-plan). 
  This document assumes you're using the [demo project](https://docs.kontent.ai/tutorials/set-up-kontent/projects/manage-projects#a-creating-a-sample-project).
* Get the [Project ID](https://docs.kontent.ai/tutorials/develop-apps/get-content/get-content-items#a-getting-content-items) of your project, you'll need it later
* Follow the [getting started](https://statiq.dev/framework/) steps for Statiq.Framework, then come back here to set up a pipeline
* Add `Kontent.Statiq` to the project:

```dotnet add package Kontent.Statiq --version 1.0.0-*```

* Install the [Kontent generator](https://github.com/Kentico/kontent-generators-net) tool to generate strong-typed models for your project
* Generate the models for your project into the Models folder:

```cmd
KontentModelGenerator --projectid "<your-project-id>" --outputdir Models --namespace My.Models -s true -g true
```
_The tool generates a C# class for each of the content types in your project as well as a `TypeProvider` that maps type names to C# classes and vice versa. It makes it easy to keep your code in sync with your Kontent project. It also provides all the code names for the properties on your content models so you can more easily write queries for the delivery SDK._

* Put a Razor view named `Article.cshtml` into `/input`:

```cshtml
@model My.Models.Article

<h3>@Model.Title</h3>

@Model.BodyCopy
```
_As you can see, the Razor view uses the model generated by Kontent. If you open the project in Visual Studio, you'll get code completion and feedback to help you write your view._

* Configure the Delivery client for your Kontent project

In `program.cs`, add in the service configurations needed to register the Delivery client and the custom type provider:

```csharp
public static class Program
{
    public static async Task<int> Main(string[] args) =>
        await Bootstrapper
            .Factory
            .CreateDefault(args)
            .ConfigureServices((services, config) =>
            {
                // Add the type provider
                services.AddSingleton<ITypeProvider, CustomTypeProvider>();
                // Configure Delivery SDK
                services.AddDeliveryClient(opts =>
                    opts.WithProjectId("<your-project-id>")
                        .UseProductionApi()
                        .Build());
            })
            .RunAsync();
}
```
* For the pipeline, add the following code to your project:

```c#
public class ArticlesPipeline : Pipeline
{
    public ArticlesPipeline(IDeliveryClient client)
    {
        InputModules = new ModuleList{
            // Load all articles from Kontent
            new Kontent<Article>(client), 
            // Set the output path for each article
            new SetDestination(Config.FromDocument((doc, ctx)  
              => new NormalizedPath( $"article/{doc.AsKontent<Article>().UrlPattern}.html"))),
        };

        ProcessModules = new ModuleList {
            // Pull in the Razor view
            new MergeContent(new ReadFiles(patterns: "Article.cshtml") ),
            // Render the Razor view into the content of the document
            new RenderRazor()
                // Use the strongly-typed model for the Razor view
                .WithModel(Config.FromDocument((document, context) 
                  => document.AsKontent<Article>()))
        };

        OutputModules = new ModuleList {
            new WriteFiles()
        };
    }
}
```
* Run the pipeline `dotnet run`

You should now see that for every Article in the Kontent site there's an html file in the `\output\article` folder.

This is a very basic pipeline and gives you everything you need to get started with content served from a Kontent project. But Kontent has more advanced features and some nice extras that you can leverage to get better integration and more robust code.

## Filtering content

If you need to filter out the the input document s from Kontent, it is possible to specify the query of your request using `WithQuery(IQueryParameter[])` method.

Let's say, you just want first three latest articles that have the Title element set and you want to load only a subset of.

```csharp
// ...
new Kontent<Article>(client)
    .WithQuery(
        new NotEmptyFilter("elements.title"),
        new OrderParameter("elements.post_date", SortOrder.Descending),
        new LimitParameter(3),
        new ElementsParameter("title", "summary", "post_date", "url_pattern")
    )
// ...
```

> For all filtering possibilities, take a look to the [Kontent .Net SDK docs](https://github.com/Kentico/kontent-delivery-sdk-net/wiki/Querying-content#filtering-retrieved-data).

## Inline content

Kontent allows you to have content within content. Which is very powerfull but requires a bit of work on the client side to make it work.
You basically have two options:

### Inline resolvers

These are called by the Kontent Delivery Client to transform inline content items into HTML. They're nice for simple models with very basic HTML.
Inline resolvers enable the Delivery API client to map structured content directly to HTML. This is achieved by making the property on the typed content class a string.

### Structured content

You can also use the structured content in your application. This is achieved by making the content property of type `IRichTextContent`. This allows you to render the inline content in views or what ever code is appropriate.

Both these models can be used with Statiq, it's up to your preferences.

Where ever Statiq hands you an `IDocument`, use the extension method `.AsKontent<TModel>()` to get the typed model from the document.

## Working with Inline resolvers (optional)

_TODO_

## Working with images (optional)

Kontent can also manage your images. It hase a very comprehensive set of [image manipulations](https://docs.kontent.ai/reference/image-transformation) baked into the Delivery API and you can leverage that with Statiq.

For example:

```html
<img src="@Model.TeaserImage.First().ImageUrl().WithWidth(350).WithHeight(350).WithFitMode(ImageFitMode.Crop).Url"/>
```

In this example `TeaserImage` is an Asset field on a strong-typed model. We pick the first asset and then the `ImageUrl` extension method provides easy access to the [ImageUrlBuilder](https://github.com/Kentico/kontent-delivery-sdk-net/blob/master/Kentico.Kontent.ImageTransformation/ImageUrlBuilder.cs) from the Kontent .NET SDK.

The `ImageUrlBuilder` provides a fluent API to specify image manipulations including resizing and a focus point. Finally, the `Url` property returns the full URL.

### Remote or local images?
Now you can leave it at that and get them served up from the Kontent API. For the full static site experience however, you can also download the images and store them in the site.

Kontent Statiq contains the `KontentImageProcessor` module that will pickup all images in your HTML content. It will generate a local unique URL for each image and replace the original url with the local url.
After that you can use the `KontentDownloadImages` module to download the images. Using the Statiq built-in task you can write them to disk.

> These modules will process _all_ images in your html. Use the `WithFilter` extension to limit what images processed and downloaded into your application.

If you're following along with the sample project:

* Process all images by adding the `KontentImageProcessor` module to the end of the `ProcessModules` phase of the Article pipeline (see demo code above for full listing).
```csharp
...
ProcessModules = new ModuleList {
        new MergeContent(new ReadFiles(patterns: "Article.cshtml") ),
        new RenderRazor()
            .WithModel(Config.FromDocument((document, context) => document.AsKontent<My.Models.Article>())),

        // Get urls from all images and replace with a local path
        new KontentImageProcessor()
    };
...
```

* Now create a new pipeline:
```csharp
public class DownloadImages : Pipeline
{
    public DownloadImages()
    {
        Dependencies.Add(nameof(Articles)); 

        PostProcessModules = new ModuleList(
            // Pull documents from other pipelines
            new ReplaceDocuments(Dependencies.ToArray()),
            // Download the images 
            new KontentDownloadImages()
        );
        OutputModules = new ModuleList(
            // Write the collected images to disk
            new WriteFiles()
        );
    }
}
```

The `KontentImageProcessor` will add any replaced image url to a collection in the Document metadata for processing by the `KontentDownloadImages` module at a later stage.

## Troubleshooting

> There are weird object tags like this in my content: 

```xml
<object type="application/kenticocloud" data-type="item" data-rel="component" data-codename="n2ef9e997_4691_0118_8777_c0ac9cee683b"></object>
```

Make sure you read the section on structured content and follow the configuration steps.

> Links to other pages don't work

Implement an register a link resolver. See the [Kontent docs](https://github.com/Kentico/kontent-delivery-sdk-net/wiki/Resolving-Links-to-Content-Items) for more information.

## How do I build this repo?

You'll need a .NET Core development setup: Windows, Mac, Linux with VisualStudio, VS Code or Rider.

## Contribution guidelines

* You're welcome to send pull requests. Please create an issue first and include a unit test with your pull request to verify your code.
* Massive refactorings, code cleanups etc. will be rejected unless explictly agreed upon
* Adding additional Nuget package dependencies to the main assemblies is strongly discouraged
* Please read and respect the [code of conduct](CODE_OF_CONDUCT.md)

## Who do I talk to?

* Marnix van Valen on twitter : @marnixvanvalen or e-mail : marnix [at] alanta [dot] nl

## Blog posts & docs

[Static sites with Kentico Cloud, Statiq and Netlify](https://www.kenticotricks.com/blog/static-sites-with-kentico-cloud) Kristian Bortnik, 31 jan 2018

