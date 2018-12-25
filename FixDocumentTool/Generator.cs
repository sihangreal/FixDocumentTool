using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FixDocumentTool
{
    public class Generator
    {
        #region 变量
        private const string ENTER = "\n";
        private const string DOUBLEENTER = "\n\n";
        private const string SPACE20 = "                    ";
        private const string SPACE16 = "                ";
        private const string SPACE12 = "            ";
        private const string SPACE8 = "        ";
        private const string SPACE4 = "    ";
        private const string SPACE = " ";
        private const string NULL = "NULL";
        private const string QUOTATION = "\"";

        private const string FixNameSpace = "QuickFix.Fields";
        private const string NAMESPACE = "namespace QuickFix";

        private Dictionary<string, FieldType> _fixTypeDic = new Dictionary<string, FieldType>();

        private List<Message> _messages = new List<Message>();
        private List<Group> _groups = new List<Group>();
        private List<BaseField> _baseFields = new List<BaseField>();
        private List<Component> _components = new List<Component>();

        private string _inFixPath;
        private string _outFixPath;
        #endregion

        #region 构造函数
        public Generator()
        {
            InitFixFieldTypeDic();
            GetFixPath();
        }
        #endregion

        #region 对外方法
        public void StartGenerator()
        {
            List<string> filePaths = new List<string>();
            DirectoryInfo direInfo = new DirectoryInfo(_inFixPath);
            foreach (FileInfo f in direInfo.GetFiles())
            {
                filePaths.Add(f.FullName);
            }

            foreach (string filePath in filePaths)
            {
                string fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1, (filePath.LastIndexOf(".") - filePath.LastIndexOf("\\") - 1)); //文件名
                XmlDocument xmlDoc = LoadXmlDocument(filePath);

                GetMessage(xmlDoc);
                GetBaseFileds(xmlDoc);
                GetComponent(xmlDoc);
                GetGroups(xmlDoc);

                GeneratorMessageCode(fileName);
                GeneratorFieldCode(fileName);
                GeneratorFieldTagCode(fileName);
                GeneratorMessageClass(fileName);
                GeneratorMessageFactory(fileName);
                _messages.Clear();
                _groups.Clear();
                _baseFields.Clear();
                _components.Clear();
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 生成MessageFactory代码
        /// </summary>
        /// <param name="fileName"></param>
        private void GeneratorMessageFactory(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("// This is a generated file.  Don't edit it directly!" + DOUBLEENTER);

            sb.Append(NAMESPACE + "." + fileName + ENTER);
            sb.Append("{" + ENTER);
            sb.Append(SPACE4 + "public class MessageFactory : IMessageFactory" + ENTER);
            sb.Append(SPACE4 + "{" + ENTER);
            //Message Create
            sb.Append(SPACE8 + "public QuickFix.Message Create(string beginString, string msgType)" + ENTER);
            sb.Append(SPACE8 + "{" + ENTER);
            sb.Append(SPACE12 + "switch(msgType)" + ENTER);
            sb.Append(SPACE12 + "{" + ENTER);
            foreach (Message message in _messages)
            {
                sb.Append(SPACE16 + "case QuickFix." + fileName + "." + message.Name + ".MsgType: return new QuickFix." + fileName + "." + message.Name + "();" + ENTER);
            }
            sb.Append(SPACE12 + "}" + ENTER);
            sb.Append(SPACE12 + "return new QuickFix.Message();" + ENTER);
            sb.Append(SPACE8 + "}" + DOUBLEENTER);

            //Group Create
            sb.Append(SPACE8 + "public Group Create(string beginString, string msgType, int correspondingFieldID)" + ENTER);
            sb.Append(SPACE8 + "{" + ENTER);

            var msgList = _messages.Where(v => v.Groups.Count > 0).ToList();
            foreach (var msg in msgList)
            {
                sb.Append(SPACE12 + "if(QuickFix." + fileName + "." + msg.Name + ".MsgType.Equals(msgType))" + ENTER);
                sb.Append(SPACE12 + "{" + ENTER);
                sb.Append(SPACE16 + "switch (correspondingFieldID)" + ENTER);
                sb.Append(SPACE16 + "{" + ENTER);
                foreach (var group in msg.Groups)
                {
                    sb.Append(SPACE20 + "case QuickFix.Fields.Tags." + group.Name + ": return new QuickFix." + fileName + "." + msg.Name + "." + group.Name + "Group();" + ENTER);

                    foreach (var childGroup in group.Groups)
                    {
                        sb.Append(SPACE20 + "case QuickFix.Fields.Tags." + childGroup.Name + ": return new QuickFix." + fileName + "." + msg.Name + "." + group.Name + "Group." + childGroup.Name + "Group();" + ENTER);
                    }
                }
                sb.Append(SPACE16 + "}" + ENTER);
                sb.Append(SPACE12 + "}" + DOUBLEENTER);
            }
            sb.Append(SPACE12 + "return null;"+ENTER);
            sb.Append(SPACE8 + "}" + ENTER);

            sb.Append(SPACE4 + "}" + ENTER);
            sb.Append("}");

            CreateCsFile(sb, fileName, "MessageFactory");
        }

        /// <summary>
        /// 生成MessageClass类
        /// </summary>
        /// <param name="fileName"></param>
        private void GeneratorMessageClass(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("// This is a generated file.  Don't edit it directly!" + DOUBLEENTER);

            sb.Append(NAMESPACE + "." + fileName + ENTER);
            sb.Append("{" + ENTER);
            sb.Append(SPACE4 + "public abstract class Message : QuickFix.Message" + ENTER);
            sb.Append(SPACE4 + "{" + ENTER);
            sb.Append(SPACE8 + "public Message():: base()" + ENTER);
            sb.Append(SPACE8 + "{" + ENTER);
            sb.Append(SPACE12 + "this.Header.SetField(new QuickFix.Fields.BeginString(QuickFix.FixValues.BeginString." + fileName + "));" + ENTER);
            sb.Append(SPACE8 + "}" + ENTER);
            sb.Append(SPACE4 + "}" + ENTER);
            sb.Append("}");

            CreateCsFile(sb, fileName, "Message");
        }

        /// <summary>
        /// 生成Message类
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="messages"></param>
        /// <param name="baseFileds"></param>
        /// <returns></returns>
        private void GeneratorMessageCode(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Message message in _messages)
            {
                sb.Append("// This is a generated file.  Don't edit it directly!" + DOUBLEENTER);

                #region 命名空间
                sb.Append("using " + FixNameSpace + ";");
                sb.Append(ENTER);
                sb.Append(NAMESPACE + "." + fileName + ENTER);
                sb.Append("{" + ENTER);
                #endregion

                sb.Append(SPACE4+"/// <summary>"+ENTER);
                sb.Append(SPACE4+"/// MsgType = "+QUOTATION + message.MsgType + QUOTATION + ENTER);
                sb.Append(SPACE4+"/// </summary>"+ENTER);

                #region 类名
                sb.Append(SPACE4 + "public class " + message.Name + " : Message" + ENTER);
                sb.Append(SPACE4 + "{" + ENTER);
                sb.Append(SPACE8 + "public const string MsgType = " + QUOTATION + message.MsgType + QUOTATION + ";");
                sb.Append(DOUBLEENTER);
                #endregion

                #region 构造函数
                //构造函数 1
                sb.Append(SPACE8 + "#region 构造函数" + ENTER);

                sb.Append(SPACE8 + "public " + message.Name + "() : base()" + ENTER);
                sb.Append(SPACE8 + "{" + ENTER);
                sb.Append(SPACE12 + "this.Header.SetField(new QuickFix.Fields.MsgType(" + QUOTATION + message.MsgType + QUOTATION + "));" + ENTER);
                sb.Append(SPACE8 + "}" + DOUBLEENTER);

                //构造函数 2
                List<Field> fieldList = message.Fields.Where(v => v.Required.Equals("Y")).ToList();
                if (fieldList.Count > 0)
                {
                    sb.Append(SPACE8 + "public " + message.Name + "(" + ENTER);
                    foreach (Field field in fieldList)
                    {
                        sb.Append(SPACE16 + "QuickFix.Fields." + field.Name + SPACE + GetVariable(field.Name) + "," + ENTER);
                    }
                    sb.Remove(sb.Length - 2, 1);
                    sb.Append(SPACE8 + ") : this()" + ENTER);
                    sb.Append(SPACE8 + "{" + ENTER);
                    foreach (Field field in fieldList)
                    {
                        sb.Append(SPACE12 + "this." + field.Name + " = " + GetVariable(field.Name) + ";" + ENTER);
                    }
                    //sb.Remove(sb.Length - 1, 1);
                    sb.Append(SPACE8 + "}" + DOUBLEENTER);
                }
                sb.Append(SPACE8 + "#endregion" + DOUBLEENTER);
                #endregion

                #region 各个字段
                sb.Append(SPACE8 + "#region 字段属性" + ENTER);
                foreach (Field field in message.Fields)
                {
                    GeneratorCode(ref sb, field,false);
                }
                #endregion


                sb.Append(SPACE8 + "#endregion" + DOUBLEENTER);

                //group 放到最后处理 找出是group的field

                sb.Append(SPACE8 + "#region Group内部类" + ENTER);
                List<Group> groupList = new List<Group>();
                foreach (var field in message.Fields)
                {
                    var group = _groups.Where(v => v.Name.Equals(field.Name)).FirstOrDefault();
                    if (group == null)
                        continue;
                    groupList.Add(group);
                }
                GeneratorGroup(ref sb, groupList,false);

                sb.Append(SPACE8 + "#endregion" + DOUBLEENTER);

                sb.Append(SPACE4 + "}" + ENTER);

                sb.Append("}" + ENTER);

                CreateCsFile(sb, fileName, message.Name);

                sb.Clear();
            }
        }

        private void GeneratorGroup(ref StringBuilder sb, List<Group> groupList, bool isIndent)
        {
            string indent = "";
            if (isIndent)
            {
                indent = "    ";
            }
            foreach (var group in groupList)
            {
                sb.Append(SPACE8 + indent + "public class " + group.Name + "Group : Group" + ENTER);
                sb.Append(SPACE8 + indent + "{" + ENTER);
                sb.Append(SPACE12 + indent + "public static int[] fieldOrder = {");
                foreach (var gfield in group.Fields)
                {
                    sb.Append("Tags." + gfield.Name + ", ");
                }
                //sb.Remove(sb.Length-1,1);
                sb.Append("0 };" + DOUBLEENTER);
                //Group 构造函数
                sb.Append(SPACE12 + indent + "public " + group.Name + "Group()" + ENTER);
                sb.Append(SPACE12 + indent + "  :base(");
                //foreach (Field gfield in group.Fields)
                //{
                //    sb.Append("Tags." + gfield.Name + ",");
                //}
                string firstField = group.Fields.Count > 0 ? ", Tags."+group.Fields[0].Name : "";
                sb.Append("Tags."+ group.Name+firstField);
                sb.Append(", fieldOrder)" + ENTER);
                sb.Append(SPACE12 + indent + "{" + ENTER);
                sb.Append(SPACE12 + indent + "}" + DOUBLEENTER);
                //Group
                sb.Append(SPACE12 + indent + "public override Group Clone()" + ENTER);
                sb.Append(SPACE12 + indent + "{" + ENTER);
                sb.Append(SPACE16 + indent + "var clone = new " + group.Name + "Group();" + ENTER);
                sb.Append(SPACE16 + indent + "clone.CopyStateFrom(this);" + ENTER);
                sb.Append(SPACE16 + indent + "return clone;" + ENTER);
                sb.Append(SPACE12 + indent + "}" + DOUBLEENTER);
                //Group 字段属性
                foreach (Field gfield in group.Fields)
                {
                    GeneratorCode(ref sb, gfield,true);
                }
                if (group.Groups != null && group.Groups.Count > 0)
                {
                    sb.Append(SPACE12 + indent + "#region Group内部类" + ENTER);
                    GeneratorGroup(ref sb, group.Groups, true);
                    sb.Append(SPACE12 + indent + "#endregion" + DOUBLEENTER);
                }

                sb.Append(SPACE8 + indent + "}" + DOUBLEENTER);
            }
        }

        /// <summary>
        /// 生成字段代码
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="field"></param>
        private void GeneratorCode(ref StringBuilder sb, Field field, bool isIndent)
        {
            string indent = "";
            if (isIndent)
            {
                indent = "    ";
            }
            //属性
            sb.Append(SPACE8+ indent + "#region ");
            BaseField baseField = _baseFields.FirstOrDefault(v => v.Name.Equals(field.Name));
            if (baseField != null)
            {
                string tag = baseField.Number;
                sb.Append("tag = " + tag);
                if (field.Required.Equals("Y"))
                    sb.Append(" 必填");
            }
            sb.Append(ENTER);

            Component componet = GetComponentByName(field.Name);
            if (componet == null)
            {
                sb.Append(SPACE8 + indent + "public QuickFix.Fields." + field.Name + SPACE + field.Name + ENTER);
                sb.Append(SPACE8 + indent + "{" + ENTER);
                sb.Append(SPACE12 + indent + "get" + ENTER);
                sb.Append(SPACE12 + indent + "{" + ENTER);
                sb.Append(SPACE16 + indent + "QuickFix.Fields." + field.Name + " val = new QuickFix.Fields." + field.Name + "();" + ENTER);
                sb.Append(SPACE16 + indent + "GetField(val);" + ENTER);
                sb.Append(SPACE16 + indent + "return val;" + ENTER);
                sb.Append(SPACE12 + indent + "}" + ENTER);
                sb.Append(SPACE12 + indent + "set { SetField(value); }" + ENTER);
                sb.Append(SPACE8 + indent + "}" + DOUBLEENTER);

                //Set
                sb.Append(SPACE8 + indent + "public void Set(QuickFix.Fields." + field.Name + " val)" + ENTER);
                sb.Append(SPACE8 + indent + "{" + ENTER);
                sb.Append(SPACE12 + indent + "this." + field.Name + " = val;" + ENTER);
                sb.Append(SPACE8 + indent + "}" + DOUBLEENTER);

                //Get
                sb.Append(SPACE8 + indent + "public QuickFix.Fields." + field.Name + " Get(QuickFix.Fields." + field.Name + " val)" + ENTER);
                sb.Append(SPACE8 + indent + "{" + ENTER);
                sb.Append(SPACE12+ indent + "GetField(val);" + ENTER);
                sb.Append(SPACE12+ indent + "return val;" + ENTER);
                sb.Append(SPACE8 + indent + "}" + DOUBLEENTER);

                //IsSet
                sb.Append(SPACE8 + indent + "public bool IsSet(QuickFix.Fields." + field.Name + " val)" + ENTER);
                sb.Append(SPACE8 + indent + "{" + ENTER);
                sb.Append(SPACE12 + indent + " return IsSet" + field.Name + "();" + ENTER);
                sb.Append(SPACE8 + indent + "}" + DOUBLEENTER);

                //IsSet Field
                sb.Append(SPACE8 + indent + "public bool IsSet" + field.Name + "()" + ENTER);
                sb.Append(SPACE8 + indent + "{" + ENTER);
                sb.Append(SPACE12 + indent + " return IsSetField(Tags." + field.Name + ");" + ENTER);
                sb.Append(SPACE8 + indent + "}" + DOUBLEENTER);

                sb.Append(SPACE8 + indent + "#endregion " + DOUBLEENTER);
            }
            else
            {
                foreach (Field cfied in componet.Fields)
                {
                    GeneratorCode(ref sb, cfied,true);
                    Group group = GetGroupByName(cfied.Name);
                    if (group != null)
                    {
                        GeneratorCode(ref sb, cfied, true);
                    }
                }
            }

        }

        /// <summary>
        /// 生成Field类
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="baseFileds"></param>
        /// <returns></returns>
        private void GeneratorFieldCode(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("// This is a generated file.  Don't edit it directly!" + DOUBLEENTER);
            sb.Append("using System;" + DOUBLEENTER);
            sb.Append("namespace QuickFix.Fields" + ENTER);
            sb.Append("{" + ENTER);
            foreach (BaseField baseFiled in _baseFields)
            {
                sb.Append(SPACE4 + "/// <summary>" + ENTER);
                sb.Append(SPACE4 + "/// " + baseFiled.Name + " Field" + ENTER);
                sb.Append(SPACE4 + "/// </summary>" + ENTER);
                sb.Append(SPACE4 + "public sealed class " + baseFiled.Name + " : ");
                sb.Append(_fixTypeDic[baseFiled.Type].ExtendType + ENTER);
                sb.Append(SPACE4 + "{" + ENTER);
                sb.Append(SPACE8 + "public " + baseFiled.Name + "()" + ENTER);
                sb.Append(SPACE12 + ":base(Tags." + baseFiled.Name + ") {}" + ENTER);
                sb.Append(SPACE8 + " public " + baseFiled.Name + "(" + _fixTypeDic[baseFiled.Type].BaseType + " val)" + ENTER);
                sb.Append(SPACE12 + ":base(Tags." + baseFiled.Name + ", val) {}" + ENTER);
                if (baseFiled.Enumerations.Count > 0)
                {
                    sb.Append(ENTER);
                    foreach (Enumeration en in baseFiled.Enumerations)
                    {
                        sb.Append(SPACE8 + "public const char " + en.Description + " = '" + en.Enum + "';" + ENTER);
                    }
                    sb.Remove(sb.Length - 1, 1);
                }
                sb.Append(SPACE4 + "}" + DOUBLEENTER);
            }
            sb.Append("}");

            CreateCsFile(sb, fileName, "Fields");
        }

        /// <summary>
        /// 生成Tag类
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="baseFileds"></param>
        /// <returns></returns>
        private void GeneratorFieldTagCode(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("// This is a generated file.  Don't edit it directly!" + DOUBLEENTER);
            sb.Append("using System;" + DOUBLEENTER);
            sb.Append("namespace QuickFix.Fields" + ENTER);

            sb.Append("{" + ENTER);

            sb.Append(SPACE4 + "/// <summary>" + ENTER);
            sb.Append(SPACE4 + "/// FIX Field Tag Values" + ENTER);
            sb.Append(SPACE4 + "/// </summary>" + ENTER);

            sb.Append(SPACE4 + "public static class Tags" + ENTER);
            sb.Append(SPACE4 + "{" + ENTER);
            foreach (BaseField baseField in _baseFields)
            {
                sb.Append(SPACE8 + "public const int " + baseField.Name + " = " + baseField.Number + ";" + ENTER);
            }
            sb.Append(SPACE4 + "}" + ENTER);
            sb.Append("}");
            CreateCsFile(sb, fileName, "FieldTags");
        }

        /// <summary>
        /// 获取Groups
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        public void GetGroups(XmlDocument xmlDoc)
        {
            XmlNodeList nodeList = xmlDoc.SelectNodes("/fix/messages/message/group");
            _groups = GetGroups(nodeList);
        }

        /// <summary>
        /// 获取Groups
        /// </summary>
        /// <param name="nodeList"></param>
        public List<Group> GetGroups(XmlNodeList nodeList)
        {
            List<Group> groups = new List<Group>();
            foreach (XmlNode node in nodeList)
            {
                Group group = new Group();
                group.Name = node.Attributes["name"].Value == null ? "" : node.Attributes["name"].Value;
                group.ParentName = node.ParentNode.Attributes["name"].Value == null ? "" : node.ParentNode.Attributes["name"].Value;

                group.Fields = new List<Field>();
                group.Groups = new List<Group>();
                XmlNodeList childNodeList = node.ChildNodes;
                foreach (XmlNode childNode in childNodeList)
                {
                    Field field = new Field();
                    field.Name = childNode.Attributes["name"].Value == null ? "" : childNode.Attributes["name"].Value;
                    field.Required = childNode.Attributes["required"].Value == null ? "" : childNode.Attributes["required"].Value;
                    group.Fields.Add(field);

                    if (childNode.Name.Equals("group"))
                    {
                        Group chilGroup = new Group();
                        chilGroup.Name = childNode.Attributes["name"].Value == null ? "" : childNode.Attributes["name"].Value;
                        chilGroup.ParentName = childNode.ParentNode.Attributes["name"].Value == null ? "" : childNode.ParentNode.Attributes["name"].Value;

                        chilGroup.Fields = new List<Field>();
                        foreach (XmlNode gNode in childNode.SelectNodes("field"))
                        {
                            Field gfiled = new Field();
                            gfiled.Name = gNode.Attributes["name"].Value == null ? "" : gNode.Attributes["name"].Value;
                            gfiled.Required = gNode.Attributes["required"].Value == null ? "" : gNode.Attributes["required"].Value;
                            chilGroup.Fields.Add(gfiled);
                        }
                        group.Groups.Add(chilGroup);

                        //chilGroup.Groups = GetGroups(childNode.ChildNodes);
                    }
                }
                groups.Add(group);
            }
            return groups;
        }

        /// <summary>
        /// 获取Component
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        private void GetComponent(XmlDocument xmlDoc)
        {
            XmlNodeList nodeList = xmlDoc.SelectNodes("/fix/components/component");
            foreach (XmlNode node in nodeList)
            {
                Component component = new Component();
                component.Name = node.Attributes["name"].Value == null ? "" : node.Attributes["name"].Value;
                //field 
                component.Fields = new List<Field>();
                XmlNodeList fieldNodeList = node.ChildNodes;
                foreach (XmlNode childFieldNode in fieldNodeList)
                {
                    Field field = new Field();
                    field.Name = childFieldNode.Attributes["name"].Value == null ? "" : childFieldNode.Attributes["name"].Value;
                    field.Required = childFieldNode.Attributes["required"].Value == null ? "" : childFieldNode.Attributes["required"].Value;
                    component.Fields.Add(field);
                }
            }
        }

        /// <summary>
        /// 获取基本Fileds
        /// </summary>
        private void GetBaseFileds(XmlDocument xmlDoc)
        {
            XmlNodeList nodeList = xmlDoc.SelectNodes("/fix/fields/field");
            foreach (XmlNode node in nodeList)
            {
                BaseField baseFiled = new BaseField();
                baseFiled.Number = node.Attributes["number"].Value == null ? "" : node.Attributes["number"].Value;
                baseFiled.Name = node.Attributes["name"].Value == null ? "" : node.Attributes["name"].Value;
                baseFiled.Type = node.Attributes["type"].Value == null ? "" : node.Attributes["type"].Value;
                baseFiled.Enumerations = new List<Enumeration>();
                XmlNodeList childNodeList = node.SelectNodes("value");
                foreach (XmlNode childNode in childNodeList)
                {
                    Enumeration enumeration = new Enumeration();
                    enumeration.Description = childNode.Attributes["description"].Value == null ? "" : childNode.Attributes["description"].Value;
                    enumeration.Enum = childNode.Attributes["enum"].Value == null ? "" : childNode.Attributes["enum"].Value;
                    baseFiled.Enumerations.Add(enumeration);
                }
                _baseFields.Add(baseFiled);
            }
        }

        /// <summary>
        ///获取Messge
        /// </summary>
        private void GetMessage(XmlDocument xmlDoc)
        {
            XmlNodeList nodeList = xmlDoc.SelectNodes("/fix/messages/message");
            foreach (XmlNode node in nodeList)
            {
                Message message = new Message();
                message.Name = node.Attributes["name"].Value == null ? "" : node.Attributes["name"].Value;
                message.MsgType = node.Attributes["msgtype"].Value == null ? "" : node.Attributes["msgtype"].Value;
                message.MsgCat = node.Attributes["msgcat"].Value == null ? "" : node.Attributes["msgcat"].Value;
                message.Fields = new List<Field>();
                message.Groups = new List<Group>();
                XmlNodeList childNodeList = node.ChildNodes;
                foreach (XmlNode childNode in childNodeList)
                {
                    Field field = new Field();
                    field.Name = childNode.Attributes["name"].Value == null ? "" : childNode.Attributes["name"].Value;
                    field.Required = childNode.Attributes["required"].Value == null ? "" : childNode.Attributes["required"].Value;
                    message.Fields.Add(field);
                }
                message.Groups = GetGroups(node.SelectNodes("group"));
                _messages.Add(message);
            }
        }

        /// <summary>
        /// 加载Fix配置XML
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private XmlDocument LoadXmlDocument(string filename)
        {
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(filename);
            }
            catch (Exception e)
            {
                //显示错误信息  
                Console.WriteLine(e.Message);
                return null;
            }
            return xmlDoc;
        }

        /// <summary>
        /// 将首字母变成小写
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string GetFirstLowerStr(string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                if (str.Length > 1)
                {
                    return char.ToLower(str[0]) + str.Substring(1);
                }
                return char.ToLower(str[0]).ToString();
            }
            return null;
        }

        /// <summary>
        /// 获取变量
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string GetVariable(string str)
        {
            return "a" + str;
        }

        /// <summary>
        /// fix路径
        /// </summary>
        private void GetFixPath()
        {
            _inFixPath = ConfigurationManager.AppSettings["InFixPath"];
            try
            {
                _outFixPath = ConfigurationManager.AppSettings["OutFixPath"];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _outFixPath = Directory.GetCurrentDirectory();
            }
        }

        /// <summary>
        /// 初始化FixFieldType类型
        /// </summary>
        private void InitFixFieldTypeDic()
        {
            _fixTypeDic.Add("STRING", new FieldType("StringField", "string"));
            _fixTypeDic.Add("CHAR", new FieldType("CharField", "char"));
            _fixTypeDic.Add("PRICE", new FieldType("DecimalField", "decimal"));
            _fixTypeDic.Add("INT", new FieldType("IntField", "int"));
            _fixTypeDic.Add("AMT", new FieldType("DecimalField", "decimal"));
            _fixTypeDic.Add("QTY", new FieldType("DecimalField", "decimal"));

            _fixTypeDic.Add("CURRENCY", new FieldType("StringField", "string"));
            _fixTypeDic.Add("MULTIPLEVALUESTRING", new FieldType("StringField", "string"));
            _fixTypeDic.Add("EXCHANGE", new FieldType("StringField", "string"));
            _fixTypeDic.Add("UTCTIMESTAMP", new FieldType("DateTimeField", "DateTime"));
            _fixTypeDic.Add("BOOLEAN", new FieldType("BooleanField", "Boolean"));

            _fixTypeDic.Add("LOCALMKTDATE", new FieldType("StringField", "string"));
            _fixTypeDic.Add("FLOAT", new FieldType("DecimalField", "decimal"));
            _fixTypeDic.Add("PRICEOFFSET", new FieldType("DecimalField", "decimal"));
            _fixTypeDic.Add("MONTHYEAR", new FieldType("StringField", "string"));
            _fixTypeDic.Add("DAYOFMONTH", new FieldType("StringField", "string"));

            _fixTypeDic.Add("UTCDATE", new FieldType("DateOnlyField", "DateTime"));
            _fixTypeDic.Add("UTCTIMEONLY", new FieldType("TimeOnlyField", "DateTime"));
            _fixTypeDic.Add("DATA", new FieldType("StringField", "string"));

            _fixTypeDic.Add("TIME", new FieldType("DateTimeField", "DateTime"));
            _fixTypeDic.Add("DATE", new FieldType("StringField", "string"));
            _fixTypeDic.Add("SEQNUM", new FieldType("IntField", "int"));
            _fixTypeDic.Add("LENGTH", new FieldType("IntField", "int"));
            _fixTypeDic.Add("NUMINGROUP", new FieldType("IntField", "int"));
            _fixTypeDic.Add("PERCENTAGE", new FieldType("DecimalField", "decimal"));

            _fixTypeDic.Add("COUNTRY", new FieldType("StringField", "string"));
            _fixTypeDic.Add("UTCDATEONLY", new FieldType("DateOnlyField", "DateTime"));
            _fixTypeDic.Add("MULTIPLECHARVALUE", new FieldType("StringField", "string"));
            _fixTypeDic.Add("MULTIPLESTRINGVALUE", new FieldType("StringField", "string"));
            _fixTypeDic.Add("TZTIMEONLY", new FieldType("StringField", "string"));
            _fixTypeDic.Add("TZTIMESTAMP", new FieldType("DateTimeField", "DateTime"));

            _fixTypeDic.Add("XMLDATA", new FieldType("StringField", "string"));
            _fixTypeDic.Add("LANGUAGE", new FieldType("StringField", "string"));
        }

        /// <summary>
        /// 生成Cs文件
        /// </summary>
        /// <param name="sb"></param>
        /// <returns></returns>
        private bool CreateCsFile(StringBuilder sb, string fileName, string messageName)
        {
            try
            {
                string filePath = _outFixPath + @"\" + fileName;
                Directory.CreateDirectory(filePath);

                string file = _outFixPath + @"\" + fileName + @"\" + messageName + ".cs";

                FileStream fs = new FileStream(file, FileMode.OpenOrCreate);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(sb);
                sw.Flush();
                sw.Close();
                //fs.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 根据名称获取Group
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Group GetGroupByName(string name)
        {
            Group group = _groups.FirstOrDefault(v => v.Name.Equals(name));
            return group;
        }

        /// <summary>
        /// 根据名称获取Component
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Component GetComponentByName(string name)
        {
            Component component = _components.FirstOrDefault(v => v.Name.Equals(name));
            return component;
        }

        #endregion
    }
}
