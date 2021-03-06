﻿using FlightNode.Common.BaseClasses;
using FlightNode.Common.Exceptions;
using FlightNode.Identity.Domain.Entities;
using FlightNode.Identity.Domain.Interfaces;
using FlightNode.Identity.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightNode.Identity.Domain.Logic
{
    public interface IUserLogic
    {
        IEnumerable<UserModel> FindAll();
        UserModel FindById(int id);
        UserModel Save(UserModel input);
        void Deactivate(int id);
        void ChangePassword(int id, PasswordModel change);
    }

    public class UserLogic : DomainLogic, IUserLogic
    {
        private IUserManager _userManager;

        public UserLogic(IUserManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            _userManager = manager;
        }


        public void Deactivate(int id)
        {
            _userManager.SoftDelete(id);
        }

        public IEnumerable<UserModel> FindAll()
        {
            var records = _userManager.Users.Where(x => x.Active);

            var dtos = Map(records);

            return dtos;                
        }

        public UserModel FindById(int id)
        {
            var record = _userManager.FindByIdAsync(id).Result;

            // TODO: Is this fully hydrated? That is, does FindByIdAsync also populate 
            // roles & claims? If so, need to map those as well.

            var dto = Map(record);

            return dto;
        }

        private UserModel Map(User input)
        {
            return new UserModel
            {
                Email = input.Email,
                MobilePhoneNumber = input.MobilePhoneNumber,
                Password = string.Empty,
                PhoneNumber = input.PhoneNumber,
                UserId = input.Id,
                UserName = input.UserName
            };
        }

        private IEnumerable<UserModel> Map(IEnumerable<User> input)
        {
            foreach(var i in input)
            {
                yield return Map(i);
            }
        }


        public UserModel Save(UserModel input)
        {
            // TODO: Need to re-assign roles / claims ??

            var record = Map<UserModel, User>(input);

            // TODO: Do we need to load the original into EF first?
            //var original = _ternRepository.FindByIdAsync(input.UserId).Result;

            
            if (input.UserId < 1)
            {
                input.UserId = SaveNew(record, input.Password);
                return input;
            }
            else
            {
                UpdateExisting(record);
                return input;
            }
        }

        private void UpdateExisting(User input)
        {
            var result = _userManager.UpdateAsync(input).Result;
            if (!result.Succeeded)
            {
                throw UserException.FromMultipleMessages(result.Errors);
            }
        }

        private int SaveNew(User record, string password)
        {
            var result = _userManager.CreateAsync(record, password).Result;
            if (result.Succeeded)
            {
                // Does this record now have the UserId in it?
                return record.Id;
            }
            else
            {
                throw UserException.FromMultipleMessages(result.Errors);
            }

        }

        public void ChangePassword(int id, PasswordModel change)
        {
            var result = _userManager.ChangePasswordAsync(id, change.OldPassword, change.NewPassword).Result;
            if (!result.Succeeded)
            {
                throw UserException.FromMultipleMessages(result.Errors);
            }
        }
    }
}
