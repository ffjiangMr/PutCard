namespace AutoAttendance
{
    #region using directive 

    using System;
    using System.Configuration;

    #endregion

    /// <summary>
    /// 用户实体类
    /// </summary>
    internal sealed class UserEntity
    {

        #region filed

        /// <summary>
        /// 用户名
        /// </summary>
        private String userName;

        /// <summary>
        /// 登陆密码
        /// </summary>
        private String password;

        /// <summary>
        /// 单例实体
        /// </summary>
        private static UserEntity instance;

        #endregion

        #region property

        /// <summary>
        /// 返回文件中配置的用户名
        /// </summary>
        public String UserName { get { return this.userName; } }

        /// <summary>
        /// 返回文件中配置的登陆密码
        /// </summary>
        public String Password { get { return this.password; } }

        #endregion

        #region Method

        /// <summary>
        /// 私有构造函数，已实现单例模式
        /// </summary>
        private UserEntity()
        {
            this.userName = ConfigurationManager.AppSettings["username"];
            this.password = ConfigurationManager.AppSettings["password"];
        }

        /// <summary>
        /// 公有实例获取方法
        /// </summary>
        /// <returns>单例实体</returns>
        public static UserEntity GetInstance()
        {
            if (instance == null)
            {
                instance = new UserEntity();
            }
            return instance;
        }

        #endregion 
    }
}
