# Async File Upload Demo #

MongoDb has a nice little feature called [GridFS](http://docs.mongodb.org/manual/core/gridfs/). [Mr. K. Scott Allen](https://twitter.com/OdeToCode) outlines things well in a post here wrote [here](http://odetocode.com/blogs/scott/archive/2013/04/16/using-gridfs-in-mongodb-from-c.aspx). What this enables us to do is to store files in a MongoDB with minimal friction! Complimenting that with my earlier post and a simple tweak to the code and we get our wonderful **async** functionality to stream to MongoDB via GridFS instead of a file directory. 

## "Showz the Codez" ##
The core to all of this is the MultipartFormDataStreamProvider class. This class is what we used in my [last post](http://www.flapstack.com/file-uploads-with-web-api-drop-the-3rd-party/) on **async** file uploads to change file name by overriding the GetFileName method. 

There two methods that we need to override to make this all possible;

First;
```language-markup
Stream MultipartFormDataStreamProvider.GetStream(HttpContent parent, HttpContentHeaders headers)
```
This method gives us the ability to return a MongoDB.Driver.GridFS.MongoGridFSStream object, which inherits Stream. And second;

```language-markup
async Task MultipartFormDataStreamProvider.ExecutePostProcessingAsync()
```
Since we need to keep in mind some form posts may include mixed data, form field data and file data. We want to be able to process them accordingly. 

The best way to understand what is going on in .NET is looking at the code. That being said take a gander at the MultipartFormDataStreamProvider class [here](http://aspnetwebstack.codeplex.com/SourceControl/changeset/view/b4631c0ef899fdccf210cda4c0e39591e67537b7#src/System.Net.Http.Formatting/MultipartFormDataStreamProvider.cs).

Our implementation is very similar with the exception of the Stream returned, in the presences of file data, is a MongoGridFSStream object instead of a FileStream object.