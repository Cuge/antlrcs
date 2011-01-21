/*
 * [The "BSD licence"]
 * Copyright (c) 2011 Terence Parr
 * All rights reserved.
 *
 * Conversion to C#:
 * Copyright (c) 2011 Sam Harwell, Tunnel Vision Laboratories, LLC
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Antlr4.StringTemplate
{
    using Antlr4.StringTemplate.Compiler;
    using IOException = System.IO.IOException;
    using ArgumentException = System.ArgumentException;
    using Exception = System.Exception;
    using Thread = System.Threading.Thread;
    using Directory = System.IO.Directory;
    using Uri = System.Uri;
    using Path = System.IO.Path;
    using FileNotFoundException = System.IO.FileNotFoundException;
    using Antlr4.StringTemplate.Misc;
    using UriFormatException = System.UriFormatException;
    using Antlr.Runtime;
    using Encoding = System.Text.Encoding;
    using NotImplementedException = System.NotImplementedException;
    using StreamReader = System.IO.StreamReader;

    // TODO: caching?

    /** A directory or directory tree full of templates and/or group files.
     *  We load files on-demand. If we fail to find a file, we look for it via
     *  the CLASSPATH as a resource.  I track everything with URLs not file names.
     */
    public class STGroupDir : STGroup
    {
        public string groupDirName;
        public Uri root;

        public STGroupDir(string dirName)
            : this(dirName, '<', '>')
        {
        }

        public STGroupDir(string dirName, char delimiterStartChar, char delimiterStopChar)
            : base(delimiterStartChar, delimiterStopChar)
        {
            this.groupDirName = dirName;
            try
            {
                throw new NotImplementedException();
#if false
                //File dir = new File(dirName);
                if (Directory.Exists(dirName))
                {
                    // we found the directory and it'll be file based
                    root = dir.toURI().toURL();
                }
                else
                {
                    ClassLoader cl = Thread.CurrentThread.getContextClassLoader();
                    root = cl.getResource(dirName);
                    if (root == null)
                    {
                        cl = this.GetType().getClassLoader();
                        root = cl.getResource(dirName);
                    }
                    if (root == null)
                    {
                        throw new ArgumentException("No such directory: " + dirName);
                    }
                }
#endif
            }
            catch (Exception e)
            {
                errMgr.internalError(null, "can't load group dir " + dirName, e);
            }
        }

        public STGroupDir(string dirName, Encoding encoding)
            : this(dirName, encoding, '<', '>')
        {
        }

        public STGroupDir(string dirName, Encoding encoding, char delimiterStartChar, char delimiterStopChar)
            : this(dirName, delimiterStartChar, delimiterStopChar)
        {
            this.encoding = encoding;
        }

        public STGroupDir(Uri root, Encoding encoding, char delimiterStartChar, char delimiterStopChar)
            : base(delimiterStartChar, delimiterStopChar)
        {
            this.root = root;
            this.encoding = encoding;
        }

        /** Load a template from dir or group file.  Group file is given
         *  precedence over dir with same name.
         */
        protected override CompiledST load(string name)
        {
            string parent = Utility.getPrefix(name);

            Uri groupFileURL = null;
            try
            { // see if parent of template name is a group file
                groupFileURL = new Uri(root + parent + ".stg");
            }
            catch (UriFormatException e)
            {
                errMgr.internalError(null, "bad URL: " + root + parent + ".stg", e);
                return null;
            }

            throw new NotImplementedException();
#if false
            InputStream @is = null;
            try
            {
                @is = groupFileURL.openStream();
            }
            catch (FileNotFoundException fnfe)
            {
                // must not be in a group file
                return loadTemplateFile(parent, name + ".st"); // load t.st file
            }
            catch (IOException ioe)
            {
                errMgr.internalError(null, "can't load template file " + name, ioe);
            }

            try
            {
                // clean up
                if (@is != null)
                    @is.close();
            }
            catch (IOException ioe)
            {
                errMgr.internalError(null, "can't close template file stream " + name, ioe);
            }

            loadGroupFile(parent, root + parent + ".stg");

            CompiledST template;
            templates.TryGetValue(name, out template);
            return template;
#endif
        }

        /** Load full path name .st file relative to root by prefix */
        public virtual CompiledST loadTemplateFile(string prefix, string fileName)
        {
            //System.out.println("load "+fileName+" from "+root+" prefix="+prefix);
            string templateName = Path.GetFileNameWithoutExtension(fileName);
            Uri f = null;
            try
            {
                f = new Uri(root + fileName);
            }
            catch (UriFormatException me)
            {
                errMgr.runTimeError(null, 0, ErrorType.INVALID_TEMPLATE_NAME, me, root + fileName);
                return null;
            }

            ANTLRReaderStream fs = null;
            try
            {
                fs = new ANTLRReaderStream(new StreamReader(f.LocalPath, encoding ?? Encoding.UTF8));
            }
            catch (IOException)
            {
                // doesn't exist; just return null to say not found
                return null;
            }

            GroupLexer lexer = new GroupLexer(fs);
            fs.name = fileName;
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            GroupParser parser = new GroupParser(tokens);
            parser.Group = this;
            lexer.group = this;
            try
            {
                parser.templateDef(prefix);
            }
            catch (RecognitionException re)
            {
                errMgr.groupSyntaxError(ErrorType.SYNTAX_ERROR, Path.GetFileName(f.LocalPath), re, re.Message);
            }

            CompiledST template;
            templates.TryGetValue(templateName, out template);
            return template;
        }

        public override string getName()
        {
            return groupDirName;
        }

        public override string getFileName()
        {
            return Path.GetFileName(root.LocalPath);
        }
    }
}