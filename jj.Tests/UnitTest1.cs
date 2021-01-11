using System;
using Xunit;
using IIG.BinaryFlag;
using IIG.PasswordHashingUtils;
using IIG.FileWorker;
using System.Collections.Specialized;
using IIG.DatabaseConnectionUtils;
using IIG.CoSFE.DatabaseUtils;
namespace jj.Tests
{
    public class UnitTest1
    {
        //---------DatabaseUtils TESTING-------------------------
         private static NameValueCollection connSettingsForFlagpoleDB = new NameValueCollection()
        {
            {"Database_ServerPath", @"DESKTOP-F2T77AN\SQLEXPRESS"},
            {"Database_DatabaseName", @"IIG.CoSWE.FlagpoleDB"},
            {"Database_TrustedConnection", "true"},
            {"Database_UserLogin", "sa"},
            {"Database_UserPassword", "123456"},
            {"Database_ConnectionTimeout", "75"}
        };

       
        private FlagpoleDatabaseUtils flagpoleDatabase = new FlagpoleDatabaseUtils(connSettingsForFlagpoleDB);


        [Fact]
        public void CheckIfConnectionSucceed()
        {
            var isConnected = flagpoleDatabase.ExecSql("select * from MultipleBinaryFlags");
            Assert.True(isConnected);

        }


        [Theory]
        [InlineData("TTTTT", true )]
        [InlineData("FFFFFFFFFF", false )]
        [InlineData("TTTTTTTTTTTFTTTTTTTTTT", false )]
        public void CkeckIfAddFlagReallyAddsFlags(string flagView, bool initValue){
            Assert.True(flagpoleDatabase.AddFlag(flagView,initValue));
            flagpoleDatabase.ExecSql("delete from MultipleBinaryFlags; DBCC CHECKIDENT (MultipleBinaryFlags, RESEED, 0);");
       
        }
        [Fact]
        public void CheckAddFlagUncompatableData(){
            Assert.False(flagpoleDatabase.AddFlag("TTTTTTFTTTTTT", true));  
        }
        [Fact]
        public void checkAddFlagViewUncorrect(){
            Assert.False(flagpoleDatabase.AddFlag("uncorrect flagView", true));
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
         public void checkAddFlagViewNullOrEmpty(string flagView){
            Assert.False(flagpoleDatabase.AddFlag(flagView, true));
        }


        [Theory]
        [InlineData(25, true)]
        [InlineData(9999, true)]
        [InlineData(9999, false)]
        public void checkAddFlagWhenFlagCreated(ulong length, bool initValue)
        {
            var mbf = new MultipleBinaryFlag(length, initValue);
            var flagView = mbf.ToString();
            var flagValue = mbf.GetFlag();
            bool isAdded = flagpoleDatabase.AddFlag(flagView, flagValue);
            Assert.True(isAdded);
            flagpoleDatabase.ExecSql("delete from MultipleBinaryFlags; DBCC CHECKIDENT (MultipleBinaryFlags, RESEED, 0);");
        }

        [Theory]
        [InlineData(2, true)]
        [InlineData(25, false)]
        [InlineData(9999, false)]
        [InlineData(9999, true)]
        public void testGetFlag(ulong length, bool initValue)
        {
            var mbf = new MultipleBinaryFlag(length, initValue);
            string flagView = mbf.ToString();
            bool flagValue = mbf.GetFlag();
            string flagViewAfterDB = "";
            bool? flagValueAfterDB = false;
            flagpoleDatabase.AddFlag(flagView,flagValue);
            bool isAdded = flagpoleDatabase.GetFlag(1, out flagViewAfterDB, out flagValueAfterDB);
            Assert.True(isAdded);

            Assert.Equal(flagView, flagViewAfterDB);
            Assert.Equal(flagValue, flagValueAfterDB);
            flagpoleDatabase.ExecSql("delete from MultipleBinaryFlags; DBCC CHECKIDENT (MultipleBinaryFlags, RESEED, 0);");
        }

        [Theory]
        [InlineData(10000, false)]
        [InlineData(10000, true)]
        public void testGetFlagWithSet(ulong length, bool initValue)
        {
            var mbf = new MultipleBinaryFlag(length, initValue);

            var rand = new Random();
            for(int i = 0, j = 0 ; i < (int)length && j < (int)length; i = rand.Next(i + 1, (int)length + 1), j = rand.Next(j + 1, (int)length + 1)){
                mbf.SetFlag((ulong)i);
                mbf.ResetFlag((ulong)i);
            }
            string flagView = mbf.ToString();
            bool flagValue = mbf.GetFlag();


            string flagViewAfterDB = "";
            bool? flagValueAfterDB = false;

            flagpoleDatabase.AddFlag(flagView, flagValue);
            bool isAdded = flagpoleDatabase.GetFlag(1, out flagViewAfterDB, out flagValueAfterDB);
            Assert.True(isAdded);
            Assert.Equal(flagView, flagViewAfterDB);
            Assert.Equal(flagValue, flagValueAfterDB);
            flagpoleDatabase.ExecSql("delete from MultipleBinaryFlags; DBCC CHECKIDENT (MultipleBinaryFlags, RESEED, 0);");
        }


        //-------------------FileWorkerUtils TESTING-------------------------
        
        private const string  dirName = "FileWorkerTestDir";
        private const string startFileName = "\\TestFile";
        private const string path = "C:\\Users\\bogda\\csProjects\\lab_4\\" + dirName;
        [Fact]
        public void TestMkDirWhenDirDoesntExist(){
            Assert.Equal(
                path,
                BaseFileWorker.MkDir(path)
            );
        }

        [Fact]
        public void TestMkDirWhenDirExists(){
            Assert.Equal(
                path,
                BaseFileWorker.MkDir(path)
            );
        }
        
        [Fact]
        public void TestWrite(){
            string hash = PasswordHasher.GetHash("sdf");
            string file = path + startFileName + "1.txt";
            Assert.True(BaseFileWorker.Write(hash, file));
        }
        [Theory]
        [InlineData("")]
        [InlineData("2.invalidFormat")]
        [InlineData(path + "")]
        public void TestWriteInvalidPath(string path){
            string hash = PasswordHasher.GetHash("sdf");
            Assert.False(BaseFileWorker.Write(hash, path));
        }

        [Fact]
        public void TestTryWrite(){
            string hash = PasswordHasher.GetHash("sdf");
            string file = path + startFileName + "1.txt";
            Assert.True(BaseFileWorker.TryWrite(hash, file,9999));
        }

        [Theory]
        [InlineData("")]
        [InlineData(path + "")]
        [InlineData(path + "\\notExistingFile.txt")]
        public void TestReadAllUncorrectPath(string file){
            var hash = BaseFileWorker.ReadAll(file);
            Assert.Null(hash);
        }
        [Theory]
        [InlineData("")]
        [InlineData(path + "")]
        [InlineData(path + "\\notExistingFile.txt")]
        public void TestReadLinesUncorrectPath(string file){
            var hash = BaseFileWorker.ReadLines(file);
            Assert.Null(hash);
        }

        [Fact]
        public void TestReadAll(){
            string hash = PasswordHasher.GetHash("sdf");
            string file = path + startFileName + "2.txt";
            BaseFileWorker.Write(hash, file);
            string hashFromFile = BaseFileWorker.ReadAll(file);
            Assert.Equal(hash,hashFromFile);
        }
        [Fact]
        public void TestReadLines(){
            string hash = PasswordHasher.GetHash("sdf");
            string file = path + startFileName + "3.txt";
            BaseFileWorker.Write(hash + "\n" + hash, file);
            string[] hashesFromFile = BaseFileWorker.ReadLines(file);
            foreach(string hashFromFile in hashesFromFile){
                Assert.Equal(hash, hashFromFile);
            }
        }

        [Fact]
        public void TestTryCopy(){
             string hash = PasswordHasher.GetHash("sdf");
            string file = path + startFileName + "4.txt";
            string fileToCopy = path + startFileName + "copied4.txt";
            BaseFileWorker.Write(hash , file);
            Assert.True(BaseFileWorker.TryCopy(file, fileToCopy, false, 9999));
            Assert.Equal(hash, BaseFileWorker.ReadAll(fileToCopy));
        }

        [Fact]
        public void TestTryCopyRewrite(){
             string hash = PasswordHasher.GetHash("sdf");
            string file = path + startFileName + "5.txt";
            string fileToCopy = path + startFileName + "copied5.txt";

            BaseFileWorker.Write(hash , file);
            BaseFileWorker.Write("some text different from possible hash", fileToCopy);
            
            Assert.True(BaseFileWorker.TryCopy(file, fileToCopy, true, 9999));
            Assert.Equal(hash, BaseFileWorker.ReadAll(fileToCopy));
        }


    }
}
