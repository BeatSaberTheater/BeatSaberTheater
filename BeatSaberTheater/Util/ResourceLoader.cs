using System;
using System.Reflection;
using UnityEngine;

namespace BeatSaberTheater.Util
{
    public class ResourceLoader
    {
        public static Lazy<Sprite> IconTrash = new Lazy<Sprite>(() => CreateSprite("BeatSaberTheater.Resources.trash-2.png"));
        public static Lazy<Sprite> IconUpload = new Lazy<Sprite>(() => CreateSprite("BeatSaberTheater.Resources.upload.png"));
        public static Lazy<Sprite> IconDownload = new Lazy<Sprite>(() => CreateSprite("BeatSaberTheater.Resources.download.png"));

        public static byte[] GetResource(string path)
        {
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            Plugin._log.Info("Resource list");
            foreach (var r in resources)
            {
                Plugin._log.Info(r);
            }
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            if (stream?.Length > 0)
            {
               var bytes = new byte[stream.Length];
               stream.Read(bytes, 0, bytes.Length);
               return bytes;
            }
            else
            {
                return [];
            }
        }

        private static Sprite CreateSprite(string path)
        {
            Plugin._log.Info("Creating sprite: " + path);
            var resource = GetResource(path);
            if (resource.Length == 0)
            {
                Plugin._log.Warn("Resource Length is 0: " + path);
            }
            var texture = new Texture2D(2, 2);
            texture.LoadImage(resource);
            var sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f), 100);
            
            if (resource.Length != 0)
            {
                Plugin._log.Info("Created sprite: " + sprite.name);
            }
            return sprite;
        }
    }
}