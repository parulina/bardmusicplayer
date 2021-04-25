﻿using BardMusicPlayer.Notate.Objects;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BardMusicPlayer.Catalog.Tests
{

    [TestClass]
    public class DBPlaylistTest : LiteDBTestBase
    {
        public DBPlaylistTest() : base() { }

        [TestMethod]
        public void TestSerialization()
        {
            string playlistName = "Test Playlist";
            ObjectId playlistId = ObjectId.NewObjectId();
            DBPlaylist test = new DBPlaylist()
            {
                Name = playlistName,
                Songs = new List<MMSong>(),
                Id = playlistId
            };

            DBPlaylist saved;

            using (var dbi = this.CreateDatabase())
            {
                var collection = dbi.GetCollection<DBPlaylist>(Constants.PLAYLIST_COL_NAME);
                collection.Insert(test);

                saved = collection.Query()
                    .Include(x => x.Songs)
                    .Where(x => x.Id.Equals(playlistId))
                    .First();
            }

            Assert.IsNotNull(saved);
            Assert.AreEqual(playlistName, saved.Name);
            Assert.AreEqual(playlistId, saved.Id);
            Assert.IsNotNull(saved.Songs);
            Assert.AreEqual(0, saved.Songs.Count);
        }

        protected override void OnInit() { }

        protected override void OnTearDown() { }
    }
}
