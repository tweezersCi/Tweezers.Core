﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Tweezers.Api.Common;
using Tweezers.Api.Controllers;
using Tweezers.Api.DataHolders;
using Tweezers.Api.Identity.DataHolders;
using Tweezers.Api.Identity.HashUtils;
using Tweezers.DBConnector;
using Tweezers.Schema.Common;
using Tweezers.Schema.DataHolders;
using Tweezers.Schema.DataHolders.Exceptions;

namespace Tweezers.Api.Identity.Controllers
{
    [Route("api")]
    [ApiController]
    public class UsersController : TweezersControllerBase
    {
        private const string UsersCollectionName = "users";
        private static readonly TweezersObject UsersLoginSchema;

        protected TimeSpan SessionTimeout => 4.Hours();

        static UsersController()
        {
            UsersLoginSchema = new TweezersObject()
            {
                CollectionName = "users",
                Internal = true,
            };

            UsersLoginSchema.DisplayNames.SingularName = "User";
            UsersLoginSchema.DisplayNames.PluralName = "Users";
            UsersLoginSchema.Icon = "person";

            UsersLoginSchema.Fields.Add("username", new TweezersField()
            {
                Name = "username",
                DisplayName = "Username",
                FieldProperties = new TweezersFieldProperties()
                {
                    FieldType = TweezersFieldType.String,
                    UiTitle = true,
                    Min = 1,
                    Max = 50,
                    Required = true,
                    Regex = @"[A-Za-z\d]+"
                }
            });

            UsersLoginSchema.Fields.Add("password", new TweezersField()
            {
                Name = "password",
                DisplayName = "Password",
                FieldProperties = new TweezersFieldProperties()
                {
                    FieldType = TweezersFieldType.Password,
                    Min = 8,
                    Max = 50,
                    Required = true,
                    GridIgnore = true,
                }
            });
        }

        private readonly string[] _loginResponseBody = 
            {"username", IdentityManager.SessionIdKey, IdentityManager.SessionExpiryKey};

        [HttpGet("tweezers-schema/users")]
        public ActionResult<TweezersObject> GetUsersSchema()
        {
            return IsSessionValid()
                ? TweezersOk(UsersLoginSchema) 
                : TweezersNotFound();
        }

        [HttpPost("users")]
        public ActionResult<JObject> Post([FromBody] LoginRequest suggestedUser)
        {
            if (!IdentityManager.UsingIdentity)
                return TweezersNotFound();

            if (!IsSessionValid())
                return TweezersUnauthorized();

            try
            {
                if (FindUser(suggestedUser.Username) != null)
                {
                    throw new TweezersValidationException(TweezersValidationResult.Reject($"Unable to create user"));
                }

                TweezersValidationResult passwordOk = UsersLoginSchema.Fields["password"].Validate(suggestedUser.Password);
                if (!passwordOk.Valid)
                {
                    throw new TweezersValidationException(passwordOk);
                }

                JObject user = new JObject
                {
                    ["username"] = suggestedUser.Username,
                    ["passwordHash"] = Hash.Create(suggestedUser.Password)
                };

                TweezersObject usersObjectMetadata = TweezersSchemaFactory.Find(UsersCollectionName, true, true);
                user = usersObjectMetadata.Create(TweezersSchemaFactory.DatabaseProxy, user, suggestedUser.Username);
                return TweezersOk(user);
            }
            catch (TweezersValidationException e)
            {
                return TweezersBadRequest(e.Message);
            }
        }

        [HttpGet("users")]
        public virtual ActionResult<TweezersMultipleResults> List()
        {
            if (!IdentityManager.UsingIdentity)
                return TweezersNotFound();

            if (!IsSessionValid())
                return TweezersUnauthorized();

            try
            {
                TweezersObject objectMetadata = TweezersSchemaFactory.Find(UsersCollectionName, true);
                IEnumerable<JObject> results = objectMetadata.FindInDb(TweezersSchemaFactory.DatabaseProxy, FindOptions<JObject>.Default());
                return TweezersOk(TweezersMultipleResults.Create(results));
            }
            catch (TweezersValidationException)
            {
                return TweezersNotFound();
            }
        }

        [HttpGet("users/{id}")]
        public virtual ActionResult<JObject> Get(string id)
        {
            if (!IdentityManager.UsingIdentity)
                return TweezersNotFound();

            if (!IsSessionValid())
                return TweezersUnauthorized();

            try
            {
                TweezersObject objectMetadata = TweezersSchemaFactory.Find(UsersCollectionName, true);
                JObject obj = objectMetadata.GetById(TweezersSchemaFactory.DatabaseProxy, id);
                if (obj == null)
                    return TweezersNotFound();

                return TweezersOk(obj);
            }
            catch (TweezersValidationException)
            {
                return TweezersNotFound();
            }
        }

        [HttpPost("login")]
        public ActionResult Login([FromBody] LoginRequest request)
        {
            if (!IdentityManager.UsingIdentity)
                return TweezersNotFound();

            try
            {
                if (Authenticate(request, out JObject user))
                {
                    string sessionId = Guid.NewGuid().ToString();
                    user[IdentityManager.SessionIdKey] = sessionId;
                    user[IdentityManager.SessionExpiryKey] = (DateTime.Now + SessionTimeout).ToFileTimeUtc()
                        .ToString(CultureInfo.InvariantCulture);

                    TweezersObject usersObjectMetadata = TweezersSchemaFactory.Find("users",
                        true, true);

                    usersObjectMetadata.Update(TweezersSchemaFactory.DatabaseProxy, user["_id"].ToString(),
                        user.Just(IdentityManager.SessionIdKey, IdentityManager.SessionExpiryKey));

                    return TweezersOk(user.Just(_loginResponseBody)).WithSecureCookie(Response, IdentityManager.SessionIdKey, sessionId);
                }

                return TweezersUnauthorized("Bad username or password");
            }
            catch (TweezersValidationException e)
            {
                return TweezersBadRequest(e.Message);
            }
        }

        [HttpDelete("users/{username}")]
        public ActionResult<bool> DeleteUser(string username)
        {
            if (!IdentityManager.UsingIdentity)
                return TweezersNotFound();

            if (!IsSessionValid())
                return TweezersUnauthorized();

            JObject user = FindUser(username);
            if (user == null)
            {
                return TweezersOk(true);
            }

            TweezersObject usersObjectMetadata = TweezersSchemaFactory.Find("users", true);
            bool deleted = usersObjectMetadata.Delete(TweezersSchemaFactory.DatabaseProxy, username);

            return TweezersOk(deleted);
        }

        [HttpPost("user/reset-password")]
        public ActionResult<bool> ResetPassword([FromBody] ChangePasswordRequest changePasswordRequest)
        {
            if (!IdentityManager.UsingIdentity)
                return TweezersNotFound();

            if (!IsSessionValid())
                return TweezersUnauthorized();

            JObject user = IdentityManager.FindUserBySessionId(Request.Headers[IdentityManager.SessionIdKey]);

            bool oldPasswordOk = ValidatePassword(changePasswordRequest.OldPassword, user["passwordHash"].ToString());
            if (!oldPasswordOk)
            {
                return TweezersBadRequest("Passwords do not match");
            }

            return DoChangePassword(user, changePasswordRequest);
        }

        [HttpPost("users/{username}/change-password")]
        public ActionResult<bool> ChangePassword(string username, [FromBody] ChangePasswordRequest changePasswordRequest)
        {
            if (!IdentityManager.UsingIdentity)
                return TweezersNotFound();

            if (!IsSessionValid())
                return TweezersUnauthorized();

            JObject user = FindUser(username);
            return user == null 
                ? TweezersNotFound() 
                : DoChangePassword(user, changePasswordRequest);
        }

        private ActionResult<bool> DoChangePassword(JObject user, ChangePasswordRequest changePasswordRequest)
        {
            JObject passwordChange = new JObject()
            {
                ["passwordHash"] = Hash.Create(changePasswordRequest.NewPassword)
            };

            try
            {
                TweezersObject usersObjectMetadata = TweezersSchemaFactory.Find("users", true);
                usersObjectMetadata.Update(TweezersSchemaFactory.DatabaseProxy, user["_id"].ToString(),
                    passwordChange);

                return TweezersOk(true);
            }
            catch
            {
                return TweezersBadRequest("Could not update password");
            }
        }

        private bool Authenticate(LoginRequest request, out JObject user)
        {
            user = FindUser(request.Username);

            if (user == null)
                return false;

            bool passwordValidated = ValidatePassword(request.Password, user["passwordHash"].ToString());

            if (!passwordValidated)
                user = null;

            return passwordValidated;
        }

        private JObject FindUser(string username)
        {
            FindOptions<JObject> userOpts = new FindOptions<JObject>()
            {
                Predicate = (u) => u["username"].ToString().Equals(username),
                Take = 1
            };

            TweezersObject usersObjectMetadata = TweezersSchemaFactory.Find("users", true, true);
            return usersObjectMetadata.FindInDb(TweezersSchemaFactory.DatabaseProxy, userOpts, true)?.SingleOrDefault();
        }

        private bool ValidatePassword(string requestPassword, string userPasswordHash)
        {
            return Hash.Validate(requestPassword, userPasswordHash);
        }
    }
}