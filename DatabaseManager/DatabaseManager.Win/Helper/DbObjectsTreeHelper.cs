﻿using DatabaseInterpreter.Core;
using DatabaseInterpreter.Model;
using DatabaseManager.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabaseManager.Helper
{
    public static class DbObjectsTreeHelper
    {
        public static readonly string FakeNodeName = "_FakeNode_";
        public static DatabaseObjectType DefaultObjectType = DatabaseObjectType.UserDefinedType | DatabaseObjectType.Table | DatabaseObjectType.View | DatabaseObjectType.Procedure | DatabaseObjectType.Function | DatabaseObjectType.TableTrigger;

        public static string GetFolderNameByDbObjectType(DatabaseObjectType databaseObjectType)
        {
            return ManagerUtil.GetPluralString(databaseObjectType.ToString());
        }

        public static string GetFolderNameByDbObjectType(Type objType)
        {
            return ManagerUtil.GetPluralString(objType.Name.ToString());
        }

        public static DatabaseObjectType GetDbObjectTypeByFolderName(string folderName)
        {
            string value = ManagerUtil.GetSingularString(folderName);
            DatabaseObjectType type = DatabaseObjectType.None;

            Enum.TryParse(value, out type);

            return type;
        }

        public static string GetImageKey(string name)
        {
            return $"tree_{name}.png";
        }

        public static TreeNode CreateTreeNode(string name, string text, string imageKeyName)
        {
            TreeNode node = new TreeNode(text);
            node.Name = name;
            node.ImageKey = GetImageKey(imageKeyName);
            node.SelectedImageKey = node.ImageKey;
            return node;
        }

        public static TreeNode CreateFolderNode(string name, string text, bool createFakeNode = false)
        {
            TreeNode node = CreateTreeNode(name, text, "Folder");

            if(createFakeNode)
            {
                node.Nodes.Add(CreateFakeNode());
            }

            return node;
        }

        public static TreeNode CreateFakeNode()
        {
            return CreateTreeNode(FakeNodeName, "", "Fake");
        }

        #region TreeNode Extension
        public static TreeNode CreateTreeNode<T>(T dbObject, bool createFakeNode = false)
            where T : DatabaseObject
        {
            TreeNode node = CreateTreeNode(dbObject.Name, dbObject.Name, typeof(T).Name);
            node.Tag = dbObject;

            if (createFakeNode)
            {
                node.Nodes.Add(CreateFakeNode());
            }

            return node;
        }

        public static TreeNode AddDbObjectNodes<T>(this TreeNode treeNode, List<T> dbObjects)
         where T : DatabaseObject
        {
            treeNode.Nodes.AddRange(CreateDbObjectNodes(dbObjects).ToArray());

            return treeNode;
        }

        public static TreeNode AddDbObjectFolderNode<T>(this TreeNode treeNode, List<T> dbObjects)
           where T : DatabaseObject
        {
            string folderName = GetFolderNameByDbObjectType(typeof(T));

            TreeNode node = CreateFolderNode(folderName, folderName, dbObjects);
            if (node != null)
            {
                treeNode.Nodes.Add(node);
                return node;
            }

            return null;
        }

        public static TreeNode AddDbObjectFolderNode<T>(this TreeNodeCollection treeNodes, string name, string text, List<T> dbObjects)
          where T : DatabaseObject
        {
            TreeNode node = CreateFolderNode(name, text, dbObjects);
            if (node != null)
            {
                treeNodes.Add(node);
                return node;
            }
            return null;
        }

        public static TreeNode CreateFolderNode<T>(string name, string text, List<T> dbObjects)
           where T : DatabaseObject
        {
            if (dbObjects.Count > 0)
            {
                var node = CreateFolderNode(name, text);

                node.Nodes.AddRange(CreateDbObjectNodes(dbObjects).ToArray());

                return node;
            }
            return null;
        }

        public static IEnumerable<TreeNode> CreateDbObjectNodes<T>(List<T> dbObjects)
           where T : DatabaseObject
        {
            foreach (var dbObj in dbObjects)
            {
                TreeNode node = CreateTreeNode(dbObj.Name, dbObj.Name, dbObj.GetType().Name);
                node.Tag = dbObj;

                yield return node;
            }
        }
        #endregion
    }
}
