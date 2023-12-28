using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProServ_ClubCore_Server_API.Database;
using ProServ_ClubCore_Server_API.DTO;
using ProServ_ClubCore_Server_API.Models;

namespace ProServ_ClubCore_Server_API.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDbContextFactory<ProServDbContext> _contextFactory;

        public MessageController(UserManager<IdentityUser> userManager, IDbContextFactory<ProServDbContext> contextFactory)
        {
            _userManager = userManager;
            _contextFactory = contextFactory;
        }

        private async Task<bool> ValidateUserConversationAccess(Guid conversationId, string userId)
        {
            try
            {
                using (var context = _contextFactory.CreateDbContext())
                {
                    var conversation = await context.DirectConversations.FirstOrDefaultAsync(dc => dc.Conversation_ID == conversationId);

                    if (conversation == null)
                    {
                        return false;
                    }

                    if (conversation.User1_ID != userId && conversation.User2_ID != userId)
                    {
                        return true;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                //TODO log error
                return false;
            }
        }

        //create new direct message thread
        [HttpPost("new-direct-message-thread")]
        [Authorize]
        public async Task<IActionResult> CreateNewDirectMessageThread([FromBody] DirectConversation_DTO newDC_DTO)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                //get user2
                var user2 = await _userManager.FindByIdAsync(newDC_DTO.User2_ID);

                if (user2 == null)
                {
                    return BadRequest("User2 was not found");
                }

                //create new direct conversation
                var newDC_Entity = new DirectConversation
                {
                    Conversation_ID = Guid.NewGuid(),
                    User1_ID = currentUser.Id,
                    User2_ID = user2.Id,
                    Date_Created = DateTimeOffset.UtcNow
                };

                //save new direct conversation
                using (var context = _contextFactory.CreateDbContext())
                {
                    //ensure there is not already a direct conversation between these two users
                    var existingDC = await context.DirectConversations.FirstOrDefaultAsync(dc => dc.User1_ID == currentUser.Id && dc.User2_ID == user2.Id);

                    if (existingDC != null)
                    {
                        return BadRequest("A direct conversation already exists between these two users");
                    }

                    await context.DirectConversations.AddAsync(newDC_Entity);
                    await context.SaveChangesAsync();

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //create new group message

        //get my message threads

        //get messages for a direct conversation
        [HttpGet("{conversationID}/messages")]
        [Authorize]
        public async Task<IActionResult> GetMessagesForDirectMessageThread([FromQuery]Guid conversationID, [FromQuery]int pageIndex, [FromQuery] int pageSize = 20)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                //get direct conversation
                using (var context = _contextFactory.CreateDbContext())
                {

                    bool valid = await ValidateUserConversationAccess(conversationID, currentUser.Id);

                    if (!valid)
                    {
                        return Unauthorized("You are not part of this direct conversation");
                    }

                    var directConversation = await context.DirectConversations.FirstOrDefaultAsync(dc => dc.Conversation_ID == conversationID);

                     //get messages for direct conversation
                     var messages = await context.DirectMessages.Where(dm => dm.Conversation_ID == conversationID).OrderByDescending(dm => dm.Timestamp).Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

                    //convert messages to DTO
                    var messages_DTO = new List<DirectMessage_DTO>();

                    foreach (var message in messages)
                    {
                        messages_DTO.Add(new DirectMessage_DTO
                        {
                            Sender_ID = message.Sender_ID,
                            Message = message.Message,
                            Timestamp = message.Timestamp
                        });
                    }

                    return Ok(messages_DTO);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        //send message to a thread
        [HttpPost("{conversationID}/send-message")]
        [Authorize]
        public async Task<IActionResult> SendMessageToDirectThread([FromQuery] Guid conversationID, [FromBody] DirectMessage_DTO message)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                       return Unauthorized("User was not found");
                }

                //get direct conversation
                using (var context = _contextFactory.CreateDbContext())
                {
                    bool valid = await ValidateUserConversationAccess(conversationID, currentUser.Id);

                    if (!valid)
                    {
                        return Unauthorized("You are not part of this direct conversation");
                    }

                    var directConversation = await context.DirectConversations.FirstOrDefaultAsync(dc => dc.Conversation_ID == conversationID);

                    //create new direct message
                    var newDirectMessage = new DirectMessage
                    {
                        Conversation_ID = conversationID,
                        Sender_ID = currentUser.Id,
                        Message = message.Message,
                        Timestamp = DateTimeOffset.UtcNow
                    };

                    //save new direct message
                    await context.DirectMessages.AddAsync(newDirectMessage);
                    await context.SaveChangesAsync();

                    //send message to all clients in the conversation
                    //TODO send message to WEBSOCKET connections is valid

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
