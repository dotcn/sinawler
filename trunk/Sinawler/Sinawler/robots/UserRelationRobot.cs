using System;
using System.Collections.Generic;
using System.Text;
using Sina.Api;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Sinawler.Model;
using System.Data;
using System.Xml;

namespace Sinawler
{
    class UserRelationRobot : RobotBase
    {
        private UserQueue queueUserForUserInfoRobot;        //用户信息机器人使用的用户队列引用
        private UserQueue queueUserForUserRelationRobot;    //用户关系机器人使用的用户队列引用
        private UserQueue queueUserForUserTagRobot;         //用户标签机器人使用的用户队列引用
        private UserQueue queueUserForStatusRobot;          //微博机器人使用的用户队列引用
        private long lQueueBufferFirst = 0;   //用于记录获取的关注用户列表、粉丝用户列表的队头值

        //构造函数，需要传入相应的新浪微博API和主界面
        public UserRelationRobot()
            : base(SysArgFor.USER_RELATION)
        {
            strLogFile = Application.StartupPath + "\\" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString() + "_userRelation.log";
            queueUserForUserInfoRobot = GlobalPool.UserQueueForUserInfoRobot;
            queueUserForUserRelationRobot = GlobalPool.UserQueueForUserRelationRobot;
            queueUserForUserTagRobot = GlobalPool.UserQueueForUserTagRobot;
            queueUserForStatusRobot = GlobalPool.UserQueueForStatusRobot;
        }

        /// <summary>
        /// 以指定的UserID为起点开始爬行
        /// </summary>
        /// <param name="lUid"></param>
        public void Start(long lStartUserID)
        {
            if (lStartUserID == 0) return;
            AdjustFreq();
            Log("The initial requesting interval is " + crawler.SleepTime.ToString() + "ms. " + api.ResetTimeInSeconds.ToString() + "s and " + api.RemainingHits.ToString()+" requests remained this hour.");

            //将起始UserID入队
            queueUserForUserRelationRobot.Enqueue(lStartUserID);
            queueUserForUserInfoRobot.Enqueue(lStartUserID);
            queueUserForUserTagRobot.Enqueue(lStartUserID);
            queueUserForStatusRobot.Enqueue(lStartUserID);
            lCurrentID = lStartUserID;

            //对队列无限循环爬行，直至有操作暂停或停止
            while (true)
            {
                if (blnAsyncCancelled) return;
                while (blnSuspending)
                {
                    if (blnAsyncCancelled) return;
                    Thread.Sleep(50);
                }
                //将队头取出
                lCurrentID = queueUserForUserRelationRobot.RollQueue();

                //日志
                Log("Recording current UserID：" + lCurrentID.ToString()+"...");
                SysArg.SetCurrentID(lCurrentID,SysArgFor.USER_RELATION);

                #region 用户关注列表
                if (blnAsyncCancelled) return;
                while (blnSuspending)
                {
                    if (blnAsyncCancelled) return;
                    Thread.Sleep(50);
                }
                //日志                
                Log("Crawling the followings of User " + lCurrentID.ToString() + "...");
                //爬取当前用户的关注的用户ID，记录关系，加入队列
                LinkedList<long> lstBuffer = crawler.GetFriendsOf(lCurrentID, -1);
                //日志
                Log(lstBuffer.Count.ToString() + " followings crawled.");

                while (lstBuffer.Count > 0)
                {
                    if (blnAsyncCancelled) return;
                    while (blnSuspending)
                    {
                        if (blnAsyncCancelled) return;
                        Thread.Sleep(50);
                    }
                    lQueueBufferFirst = lstBuffer.First.Value;
                    //若不存在有效关系，增加
                    if (!UserRelation.Exists(lCurrentID, lQueueBufferFirst))
                    {
                        if (blnAsyncCancelled) return;
                        while (blnSuspending)
                        {
                            if (blnAsyncCancelled) return;
                            Thread.Sleep(50);
                        }
                        //日志
                        Log("Recording User " + lCurrentID.ToString() + " follows User " + lQueueBufferFirst.ToString() + "...");
                        UserRelation ur = new UserRelation();
                        ur.source_user_id = lCurrentID;
                        ur.target_user_id = lstBuffer.First.Value;
                        ur.relation_state = Convert.ToInt32(RelationState.RelationExists);
                        ur.Add();
                    }
                    if (blnAsyncCancelled) return;
                    while (blnSuspending)
                    {
                        if (blnAsyncCancelled) return;
                        Thread.Sleep(50);
                    }
                    //加入队列
                    if (queueUserForUserRelationRobot.Enqueue(lQueueBufferFirst))
                        //日志
                        Log("Adding User " + lQueueBufferFirst.ToString() + " to the user queue of User Relation Robot...");
                    if (GlobalPool.UserInfoRobotEnabled && queueUserForUserInfoRobot.Enqueue(lQueueBufferFirst))
                        //日志
                        Log("Adding User " + lQueueBufferFirst.ToString() + " to the user queue of User Information Robot...");
                    if (GlobalPool.TagRobotEnabled && queueUserForUserTagRobot.Enqueue(lQueueBufferFirst))
                        //日志
                        Log("Adding User " + lQueueBufferFirst.ToString() + " to the user queue of User Tag Robot...");
                    if (GlobalPool.StatusRobotEnabled && queueUserForStatusRobot.Enqueue(lQueueBufferFirst))
                        //日志
                        Log("Adding User " + lQueueBufferFirst.ToString() + " to the user queue of Status Robot...");
                    lstBuffer.RemoveFirst();
                }

                //日志
                AdjustFreq();
                Log("Requesting interval is adjusted as " + crawler.SleepTime.ToString() + "ms." + api.ResetTimeInSeconds.ToString() + "s and " + api.RemainingHits.ToString()+" requests remained this hour.");
                #endregion
                #region 用户粉丝列表
                //爬取当前用户的粉丝的ID，记录关系，加入队列
                if (blnAsyncCancelled) return;
                while (blnSuspending)
                {
                    if (blnAsyncCancelled) return;
                    Thread.Sleep(50);
                }
                //日志
                Log("Crawling the followers of User " + lCurrentID.ToString() + "...");
                lstBuffer = crawler.GetFollowersOf(lCurrentID, -1);
                //日志
                Log(lstBuffer.Count.ToString() + " followers crawled.");

                while (lstBuffer.Count > 0)
                {
                    if (blnAsyncCancelled) return;
                    while (blnSuspending)
                    {
                        if (blnAsyncCancelled) return;
                        Thread.Sleep(50);
                    }
                    lQueueBufferFirst = lstBuffer.First.Value;
                    //若不存在有效关系，增加
                    if (!UserRelation.Exists(lQueueBufferFirst, lCurrentID))
                    {
                        if (blnAsyncCancelled) return;
                        while (blnSuspending)
                        {
                            if (blnAsyncCancelled) return;
                            Thread.Sleep(50);
                        }
                        //日志
                        Log("Recording User " + lQueueBufferFirst.ToString() + " follows User " + lCurrentID.ToString() + "...");
                        UserRelation ur = new UserRelation();
                        ur.source_user_id = lstBuffer.First.Value;
                        ur.target_user_id = lCurrentID;
                        ur.relation_state = Convert.ToInt32(RelationState.RelationExists);
                        ur.Add();
                    }
                    if (blnAsyncCancelled) return;
                    while (blnSuspending)
                    {
                        if (blnAsyncCancelled) return;
                        Thread.Sleep(50);
                    }
                    //加入队列
                    if (queueUserForUserRelationRobot.Enqueue(lQueueBufferFirst))
                        //日志
                        Log("Adding User " + lQueueBufferFirst.ToString() + " to the user queue of User Relation Robot...");
                    if (GlobalPool.UserInfoRobotEnabled && queueUserForUserInfoRobot.Enqueue(lQueueBufferFirst))
                        //日志
                        Log("Adding User " + lQueueBufferFirst.ToString() + " to the user queue of User Information Robot...");
                    if (GlobalPool.TagRobotEnabled && queueUserForUserTagRobot.Enqueue(lQueueBufferFirst))
                        //日志
                        Log("Adding User " + lQueueBufferFirst.ToString() + " to the user queue of User Tag Robot...");
                    if (GlobalPool.StatusRobotEnabled && queueUserForStatusRobot.Enqueue(lQueueBufferFirst))
                        //日志
                        Log("Adding User " + lQueueBufferFirst.ToString() + " to the user queue of Status Robot...");
                    lstBuffer.RemoveFirst();
                }
                #endregion
                //最后再将刚刚爬行完的UserID加入队尾
                //日志
                Log("Social grapgh of User " + lCurrentID.ToString() + " crawled.");
                //日志
                AdjustFreq();
                Log("Requesting interval is adjusted as " + crawler.SleepTime.ToString() + "ms." + api.ResetTimeInSeconds.ToString() + "s and " + api.RemainingHits.ToString()+" requests remained this hour.");
            }
        }

        public override void Initialize()
        {
            //初始化相应变量
            blnAsyncCancelled = false;
            blnSuspending = false;
            crawler.StopCrawling = false;
            queueUserForUserRelationRobot.Initialize();
        }

        sealed protected override void AdjustFreq()
        {
            base.AdjustRealFreq();
            SetCrawlerFreq();
        }
    }
}
