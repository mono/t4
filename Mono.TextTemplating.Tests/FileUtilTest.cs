using System;
using Mono.TextTemplating;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Mono.TextTemplating.Tests
{
	[TestClass]
	public class FileUtilTest
	{
		[TestMethod]
		public void PathResolving()
		{
			Assert.AreEqual( "something.txt", FileUtil.AbsoluteToRelativePath( "C:\\temp", "C:\\temp\\something.txt" ), "FileUtilTest" );
			Assert.AreEqual( "temp\\something.txt", FileUtil.AbsoluteToRelativePath( "C:\\", "C:\\temp\\something.txt" ), "FileUtilTest" );
			Assert.AreEqual( "temp\\subfolder\\something.txt", FileUtil.AbsoluteToRelativePath( "C:\\", "C:\\temp\\subfolder\\something.txt" ), "FileUtilTest" );
			Assert.AreEqual( "something.txt", FileUtil.AbsoluteToRelativePath( "C:\\temp\\subfolder", "C:\\temp\\subfolder\\something.txt" ), "FileUtilTest" );
		}
	}
}
