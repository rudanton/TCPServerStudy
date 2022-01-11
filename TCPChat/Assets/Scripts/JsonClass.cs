using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JsonClass
{

    public static class CMD_Constant
    {
        public const string CMD_transform = "Data:transform";
        public const string CMD_pickOne = "Data:memId";
    }
    [Serializable]
    public class parametorParser
    {
        public string type;
        public string data;
    }
    [Serializable]
    public class UserInfoList
    {
        public UserInfo[] infoList;
    }
    [Serializable]
    public class UserInfo
    {
        public string name;
        public string mem_id;
    }
    [Serializable]
    public class tranformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }
    
     
}
