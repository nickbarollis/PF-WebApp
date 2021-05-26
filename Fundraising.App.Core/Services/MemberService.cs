﻿using Fundraising.App.Core.Entities;
using Fundraising.App.Core.Interfaces;
using Fundraising.App.Core.Models;
using Fundraising.App.Core.Options;
using Fundraising.App.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fundraising.App.Core.Services
{
    public class MemberService : IMemberService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<MemberService> _logger;

        public MemberService(IApplicationDbContext dbContext, ILogger<MemberService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public OptionMember CreateMember(OptionMember optionMember)
        {
            Member member = new()
            {
                FirstName = optionMember.FirstName,
                LastName = optionMember.LastName,
                Address = optionMember.Address,
                Email = optionMember.Email,
                Username = optionMember.Username,
                Password = optionMember.Password,
                Phone = optionMember.Phone,
                Birthday = optionMember.Birthday,
                CreatedDate = DateTime.Now
            };

            _dbContext.Members.Add(member);
            _dbContext.SaveChanges();

            return new OptionMember(member);

        }

        public bool DeleteMember(int Id)
        {
            Member dbContextMember = _dbContext.Members.Find(Id);
            
            if (dbContextMember == null) return false;
            
            _dbContext.Members.Remove(dbContextMember);
            _dbContext.SaveChanges();
            return true;
        }

        public List<OptionMember> ReadAllMembers()
        {
            List<Member> members = _dbContext.Members.ToList();
            List<OptionMember> optionMembers = new();
            members.ForEach(member => optionMembers.Add(new OptionMember(member)));
            return optionMembers;
        }

        public OptionMember GetMemberById(int Id)
        {
            Member member = _dbContext.Members.Find(Id);
            if(member == null)
            {
                return null;
            }
            return new OptionMember(member);
        }

        public OptionMember UpdateMember(OptionMember optionMember, int Id)
        {
            Member dbContextMember = _dbContext.Members.Find(Id);
            if (dbContextMember == null) return null;

            // Update all members even if some of them are not changed
            dbContextMember.FirstName = optionMember.FirstName;
            dbContextMember.LastName = optionMember.LastName;
            dbContextMember.Address = optionMember.Address;
            dbContextMember.Email = optionMember.Email;
            dbContextMember.Username = optionMember.Username;
            dbContextMember.Password = optionMember.Password;
            dbContextMember.Phone = optionMember.Phone;
            dbContextMember.Birthday = optionMember.Birthday;


            _dbContext.SaveChanges();
            return new OptionMember(dbContextMember);
        }

        public async Task<Result<Member>> CreateMemberAsync(OptionMember optionMember)
        {
            // CHECK IF NOT NULL
            if (optionMember == null)
            {
                return new Result<Member>(ErrorCode.BadRequest, "Null options.");
            }

            // CHECK REQUIRED FIELDS
            if (string.IsNullOrWhiteSpace(optionMember.Email) ||
              string.IsNullOrWhiteSpace(optionMember.Phone) ||
              string.IsNullOrWhiteSpace(optionMember.Username))
            {
                return new Result<Member>(ErrorCode.BadRequest, "Not all required member options provided.");
            }

            // CHECK PHONE NUMBER LENGTH
            if (optionMember.Phone.Length > 13)
            {
                return new Result<Member>(ErrorCode.BadRequest, "Invalid phone number.");
            }

            // CHECK IF EMAIL ALREADY EXISTS IN DB
            var memberWithSameEmail = await _dbContext.Members.SingleOrDefaultAsync(cus => cus.Email == optionMember.Email);
            if (memberWithSameEmail != null)
            {
                return new Result<Member>(ErrorCode.Conflict, $"Member with #{optionMember.Email} already exists.");
            }

            // ALL CHECKS ARE OK THEN :
            // CREATE NEW MEMBER ENTRY
            var newMember = new Member
            {
                FirstName = optionMember.FirstName,
                LastName = optionMember.LastName,
                Address = optionMember.Address,
                Email = optionMember.Email,
                Username = optionMember.Username,
                Password = optionMember.Password,
                Phone = optionMember.Phone,
                Birthday = optionMember.Birthday,
                CreatedDate = DateTime.Now
            };

            // SAVE TO DATABASE
            await _dbContext.Members.AddAsync(newMember);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return new Result<Member>(ErrorCode.InternalServerError, "Could not save reward.");
            }
            return new Result<Member>
            {
                Data = newMember
            };
        }

        public async Task<Result<int>> DeleteMemberByIdAsync(int Id)
        {
            var memberToDelete = await GetMemberByIdAsync(Id);

            // CHECK IF MEMBER EXESTS
            if (memberToDelete.Error != null || memberToDelete.Data == null)
            {
                return new Result<int>(ErrorCode.NotFound, $"Member with id #{Id} not found.");
            }

            // REMOVE MEMBER FROM DATABASE
            _dbContext.Members.Remove(memberToDelete.Data);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new Result<int>(ErrorCode.InternalServerError, $"Could not delete member with id #{Id}.");
            }

            return new Result<int>
            {
                Data = Id
            };

        }

        public async Task<Result<List<Member>>> GetAllMembersAsync()
        {
            // READ ALL MEMBERS FROM DB 
            var members = await _dbContext.Members.ToListAsync();

            return new Result<List<Member>>
            {
                Data = members.Count > 0 ? members : new List<Member>()
            };
        }

        public async Task<Result<Member>> GetMemberByIdAsync(int Id)
        {
            // CHECK IF ID IS VALID 
            if (Id <= 0)
            {
                return new Result<Member>(ErrorCode.BadRequest, "Invalid ID.");
            }

            // READ MEMBER FROM DATABASE
            var member = await _dbContext
                .Members
                .SingleOrDefaultAsync(cus => cus.Id == Id);

            // CHECK IF MEMBER IS IN THE DATABASE
            if (member == null)
            {
                return new Result<Member>(ErrorCode.NotFound, $"Member with id #{Id} not found.");
            }

            // IF ALL OK RETURN FOUND MEMBER
            return new Result<Member>
            {
                Data = member
            };
        }

        public async Task<Result<Member>> UpdateMemberByIdAsync(OptionMember optionMember, int Id)
        { 
            // CHECK IF NULL
            if (optionMember == null)
            {
                return new Result<Member>(ErrorCode.BadRequest, "Null options.");
            }

            // CHECK IF PHONE NUMBER LENGTH IS VALID
            if (optionMember.Phone.Length > 13)
            {
                return new Result<Member>(ErrorCode.BadRequest, "Invalid vat number.");
            }

            // GET MEMBER TO UPDATE FROM DB
            var member = await _dbContext
                .Members
                .SingleOrDefaultAsync(cus => cus.Id == Id);

            // CHECK IF MEMBER EXISTS IN DB
            if (member == null)
            {
                return new Result<Member>(ErrorCode.NotFound, $"Member with id #{Id} not found.");
            }

            // UPDATE EVERY FIELD OF MEMBER EVEN IF IT IS NOT CHANGED
            member.FirstName = optionMember.FirstName;
            member.LastName = optionMember.LastName;
            member.Address = optionMember.Address;
            member.Email = optionMember.Email;
            member.Username = optionMember.Username;
            member.Password = optionMember.Password;
            member.Phone = optionMember.Phone;
            member.Birthday = optionMember.Birthday;
            member.CreatedDate = optionMember.CreatedDate;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return new Result<Member>(ErrorCode.InternalServerError, $"Could not update member with id #{Id}.");
            }
            return new Result<Member>
            {
                Data = member
            };
        }
    }
}
